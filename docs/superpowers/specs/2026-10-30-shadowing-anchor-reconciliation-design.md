# 高亮列表 ↔ 原声打点切片 一一对应设计（锚点对账 · 修订版）

- 状态：设计待评审（已并入评审意见）
- 日期：2026-10-30
- 语义约定：**尽力对齐（允许跳过）**——只要求“有切片的句子必须对得上、顺序不错位”；无语音的句子允许没有切片并回退文字跟读。
- 范围：只做「锚点体系 + 对账校验 + 列表按句渲染」；不含自由打点改造（见第 12 节）。
- 相对初版的变化：新增 V2 锚点、修正对账分配算法、增加修复入口与快照约束，详见第 13 节。

---

## 1. 背景与现状

### 1.1 两条打点路径

| 路径 | 入口 | 是否携带锚点 | 能否与高亮一一对应 |
| --- | --- | --- | --- |
| 原声对齐 | 跟读窗口「原声对齐」 | 是 | 能 |
| 自由打点 | 播放条「打点」 | 否 | 不能（入口已下线） |

- 原声对齐：`SentenceShadowingForm` 构造 `ShadowingCue` 列表 → `ShadowingAlignmentSession.Mark()` 逐句生成 `ShadowingSegment`，同时写入 `HighlightId / HighlightAnchor / CuePartIndex / Text`。
- 自由打点：`ShadowingMarkSession.Mark()` 只记录时间区间，ID 用随机 `CreateSegmentId`，不写文本与锚点；入口由 `PdfHiddenFeatures.EnableShadowingMark = false` 关闭，不在本设计范围内。

### 1.2 现有锚点链路

| 层 | 值 | 生成点 |
| --- | --- | --- |
| 高亮锚点 V1 | `hl:{page}:{sha256(page+规范化文本+量化Y/X+partIndex)[..16]}:{partIndex}` | `ShadowingHighlightSource.BuildAnchorKey` |
| 跟读句锚点 V1 | 有高亮：`高亮锚点 + "|" + sentence.Id`；无高亮：`txt:{page}:{sentence.Id}` | `ShadowingHighlightSource.BuildPageCues` |
| 切片 ID | `sg_{page:000}_{sha256(AnchorKey)[..8]}` | `ShadowingSegmentStore.CreateAnchoredSegmentId` |

回查顺序（`ShadowingAlignmentSession.FindExisting`）：锚点 → `HighlightId + CuePartIndex` → `Text + CuePartIndex`。

### 1.3 现状为什么会错位

1. **列表按时间排序，而不是按句子排序**
   `LoadSegments()` / `FinishAlignmentAsync()` 都执行 `_segments.OrderBy(s => s.StartMs)`，列表 `FillSentenceList()` 直接用 `_segments` 填充。一旦有句子被跳过或切片失败，列表长度就少于句子数，行 → 句的映射只能靠下标猜。
2. **句子数≠切片数时能力被整体禁用**
   构造期 `_alignmentCues = cues.Count == _textSentences.Count ? cues : Array.Empty`，数量不等就直接失去对齐能力，而不是提示差异。
3. **锚点把坐标和 `sentence.Id` 混进了哈希，天然会漂移**
   自动高亮重建 / PDF 重新渲染会改变量化坐标；重新分句会改变 `sentence.Id`。两条都会让 `hl:` / `txt:` 锚点失效，旧切片立刻变孤儿，且无法被识别。
4. **孤立切片静默堆积**
   旧切片只有在“时间范围改变”分支里会被删（`DeleteStaleSlice`），锚点不匹配的切片永远不会被识别，磁盘只增不减。

---

## 2. 目标与非目标

### 2.1 目标

- G1 本页“句子列表”与“cue 列表”**恒等**：行数 = cue 数，行顺序 = cue 顺序。
- G2 每行通过**锚点**而非下标关联切片；有切片 → 播放原声，无切片 → 回退 TTS。
- G3 对账结果可观测且**可修复**：区分「匹配成功 / 命中但缺切片 / 跳过 / 未对齐完 / 孤立切片 / 重复」，并提供行级与页级修复动作。
- G4 高亮重建、PDF 重渲染后，只要**文本未变**，切片仍能找回（V2 锚点）。
- G5 不改变用户操作，不改变 `segments.json` 的 SchemaVersion（仍为 3）。

### 2.2 非目标

- N1 不强制每条高亮必须有切片（允许 Skip）。
- N2 不在加载时自动写盘修复（加载只读，只做提示）。
- N3 不恢复 / 改造自由打点入口。
- N4 不做跨页/整篇音频的时间轴重排。
- N5 不做自动删除切片（清理必须由用户显式触发）。

---

## 3. 术语与不变量

| 术语 | 定义 |
| --- | --- |
| Cue | 一条可对齐的跟读句（`ShadowingCue`），是本设计的**唯一真源** |
| Binding | cue 与 segment 的匹配结果 |
| Orphan | 磁盘上存在、但任何 cue 都没有命中的 segment |
| 漂移 | cue 集合与 segment 集合的锚点不能再一一对应 |
| ContentKey | 只由“页 + 规范化文本 + 同文本序号”构成的身份，不含坐标、不含分句 ID |
| PositionKey | 坐标/版面信息，仅用于重复文本消歧与排序，不参与切片身份 |

必须成立的不变量：

- I1 `rows.Count == cues.Count`，`rows[i].Cue == cues[i]`。
- I2 一个 cue 最多绑定一个 segment；一个 segment 最多被一个 cue 绑定。
- I3 **分阶段全局匹配**：先让所有 cue 走最高优先级，再对未绑定 cue 降级，禁止“逐 cue 一路降级”（避免前面的句子抢占后面句子的精确命中）。
- I4 顺序只由 cue 决定；`StartMs` 只用于播放与合成，不用于列表排序。
- I5 对账是**纯函数**：不读文件、不写文件、不改入参（切片存在性由外部注入）。
- I6 幂等性与置换不变性：`Reconcile(cues, segments)` 打乱 `segments` 顺序结果不变；用结果再跑一次对账结果不变。

---

## 4. 总体方案

```
PdfHighlight[] ──BuildPageCues(V2)──▶ ShadowingCue[]  (唯一真源，含 V2 + Legacy 锚点)
                                             │
segments.json ───────────────────────────────┼──▶ ShadowingCueReconciler.Reconcile
                                             │            │
                                             │            ├─ Bindings (与 cue 等长)
                                             │            ├─ Orphans / Duplicates
                                             │            └─ DriftSummary
                                             ▼
                    列表渲染（cue 顺序，两种模式统一） + 原声/TTS + 行级修复动作
```

四层落地：

1. **锚点升级**：V2（ContentKey）为主，V1 双读。
2. **Core 新增纯函数对账器**（分阶段全局匹配 + 字典索引）。
3. **保存/加载接入**：保存前对账并写提示；加载后只读对账生成 binding 表。
4. **列表统一按 cue 渲染**，并给用户行级修复入口。

---

## 5. 详细设计

### 5.1 锚点体系 V2（核心改动）

新文件：`LearningAssistant.Core/Services/Shadowing/ShadowingAnchorKeys.cs`

```csharp
public static class ShadowingAnchorKeys
{
    /// <summary>高亮身份：页 + 规范化文本 + 同文本序号；不含坐标，抗重建/重排。</summary>
    public static string BuildHighlightV2(PdfHighlight highlight, int partIndex, int textOrdinal);

    /// <summary>页面文字身份：页 + 规范化文本 + 同文本序号；不含 sentence.Id，抗重分句。</summary>
    public static string BuildPageTextV2(int pageIndex, string text, int textOrdinal);

    /// <summary>与 cue 的合成：V2 锚点 + "|" + 组内序号（同一 base 锚点覆盖多条 cue 时按句序递增，替代 sentence.Id 保证唯一）。</summary>
    public static string BuildCueV2(string baseAnchor, int groupOrdinal);

    /// <summary>V1 兼容锚点，用于读取已写盘切片。</summary>
    public static string BuildHighlightV1(PdfHighlight highlight, int partIndex);
    public static string BuildCueV1(string baseAnchor, string sentenceId);
}
```

格式：

| 版本 | 格式 |
| --- | --- |
| V2 高亮 | `hl2:{page}:{sha256(page+规范化文本+同文本序号)[..16]}:{partIndex}` |
| V2 页面文字 | `txt2:{page}:{sha256(规范化文本+同文本序号)[..16]}` |
| V2 cue | `{baseV2}\|{组内序号}`（同 base 锚点下按句序从 0 计数；一高亮多句同文本时保证唯一） |
| V1（兼容） | 沿用 `hl:` / `txt:` 现有算法，`sentence.Id` 不改 |

- “同文本序号” = 本页内该规范化文本第 k 次出现（按句序从 0 计数），由 `BuildPageCues` 顺序计算，确定性。
- “组内序号” = 同一 base 锚点（同一高亮或同一段页面文字）覆盖多条 cue 时按句序递增的序号，替代 V1 中的 `sentence.Id`；**句文本哈希只用于诊断日志，不参与身份**——否则一条高亮覆盖两条同文本句时锚点碰撞，I2 无法成立。
- `ShadowingCue` 新增只读属性 `LegacyAnchorKey`（不持久化、不影响 Schema 版本），供双读。
- **新打点只写 V2**；V1 仅用于读取历史切片，后续版本可移除。

收益：自动高亮重建、PDF 缩放/重渲染、页面重新分句（文本边界不变）都不会再让锚点失效。

代价：同页重复文本删除其中一条会让序号平移（无法自动识别），列入风险表。

### 5.2 新增组件

新文件：`LearningAssistant.Core/Services/Shadowing/ShadowingCueReconciler.cs`

```csharp
namespace LearningAssistant.Services.Shadowing
{
    public enum CueBindingStatus
    {
        /// <summary>锚点命中，且切片文件存在。</summary>
        Matched,
        /// <summary>锚点命中，但 SlicePath 为空或切片文件缺失。</summary>
        MissingSlice,
        /// <summary>没有任何记录命中该 cue。</summary>
        Unbound,
        /// <summary>命中多条记录，取优先级最高的一条。</summary>
        Ambiguous
    }

    public enum CueUnboundReason
    {
        /// <summary>该页尚未开始/完成对齐。</summary>
        NotAlignedYet,
        /// <summary>已对齐但该句被跳过。</summary>
        Skipped
    }

    public sealed record CueBinding(
        ShadowingCue Cue,
        ShadowingSegment? Segment,
        CueBindingStatus Status,
        int CandidateCount,
        CueUnboundReason? UnboundReason);

    public sealed class ReconcileResult
    {
        /// <summary>与 cues 等长、同序。</summary>
        public IReadOnlyList<CueBinding> Bindings { get; init; } = Array.Empty<CueBinding>();

        /// <summary>未被任何 cue 命中的切片；不自动删除，仅上报。</summary>
        public IReadOnlyList<ShadowingSegment> Orphans { get; init; } = Array.Empty<ShadowingSegment>();

        /// <summary>同一 cue 命中的额外记录。</summary>
        public IReadOnlyList<ShadowingSegment> Duplicates { get; init; } = Array.Empty<ShadowingSegment>();

        public bool HasDrift => Orphans.Count > 0 || Duplicates.Count > 0
            || Bindings.Any(b => b.Status is CueBindingStatus.MissingSlice or CueBindingStatus.Ambiguous);

        public int MatchedCount => Bindings.Count(b => b.Status is CueBindingStatus.Matched or CueBindingStatus.Ambiguous);
        public int UnboundCount => Bindings.Count(b => b.Status == CueBindingStatus.Unbound);
    }

    public static class ShadowingCueReconciler
    {
        /// <summary>
        /// 分阶段全局对账；sliceExists 由调用方注入（默认 true），便于单测。
        /// </summary>
        public static ReconcileResult Reconcile(
            IReadOnlyList<ShadowingCue> cues,
            IReadOnlyList<ShadowingSegment> segments,
            Func<ShadowingSegment, bool>? sliceExists = null,
            bool alignedOnce = false);

        /// <summary>供 FindExisting 复用；只覆盖 P1–P3（逐对可判），P4 文本分组分配由 Reconcile 内部完成。返回 0 表示不匹配，1 为最高优先级。</summary>
        public static int MatchPriority(ShadowingCue cue, ShadowingSegment segment);
    }
}
```

`alignedOnce` 用于区分“未对齐完”与“已跳过”：由调用方传入“该页是否已有任意切片”。

### 5.3 匹配算法（分阶段全局匹配）

优先级定义：

| 优先级 | 条件 | 说明 |
| --- | --- | --- |
| P1 | `segment.HighlightAnchor == cue.AnchorKey` | V2 主路径 |
| P2 | `segment.HighlightAnchor == cue.LegacyAnchorKey` | 兼容未升级的 V1 数据（坐标未变时才成立，不能抵抗高亮重建） |
| P3 | `segment.HighlightId == cue.HighlightId` 且 `segment.CuePartIndex == cue.PartIndex` | 未写锚点的旧对齐切片 |
| P4 | `segment.HighlightAnchor` 为空 且 `segment.Text == cue.Text` 且 `CuePartIndex == PartIndex` | 极旧数据兜底；按文本分组分配（见下） |

**全局分阶段**（修正初版的贪心缺陷）：

```
indexes = BuildIndexes(segments)          // anchor→list, (highlightId,part)→list, text→list
bound = [null] * cues.Count
used  = HashSet<segment.Id>

foreach stage in [P1, P2, P3]:            // 逐对可判的优先级
    foreach cue i in 0..n-1:
        if bound[i] != null: continue
        candidates = indexes[stage].Lookup(cue) 排除 used
        if candidates.Empty: continue
        primary = Order(candidates).First()      // SlicePath非空 > 有Takes > Id字典序
        used.Add(primary.Id); bound[i] = primary
        duplicates += candidates.Skip(1)

// P4 文本兜底：segment 没有“同文本序号”字段，无法逐对比较，
// 改为分组分配——文本相同的未绑定 cue（按句序）与文本相同的未绑定 segment（按 StartMs）按位置一一配对。
foreach textGroup in 未绑定 cue 按规范化文本分组:
    peers = 未 used 且规范化文本相同的 segments，按 StartMs 升序
    zip 按位置配对；多余 segment 进 Orphans，多余 cue 保持 Unbound

bindings = 按 cue 顺序输出；未绑定的按 alignedOnce 决定 UnboundReason
status   = SliceUsable(primary) ? (candidates>1 ? Ambiguous : Matched) : MissingSlice
orphans  = segments 中未被 used 的，按 StartMs 排序
```

- 分阶段的意义：保证“精确命中”永远优先于“兜底命中”，不会出现第 1 句用 P4 抢占第 2 句 P1 的切片。
- 复杂度：构建 3 个字典 O(n)，逐阶段查表 O(n + 候选数)，总体 O(n)，满足整本模式接入后的规模（修正初版 O(n²)）。

### 5.4 与 `FindExisting` 的关系

`ShadowingAlignmentSession.FindExisting` 继续保留（打点过程中实时复用旧切片需要它），改为调用 `ShadowingCueReconciler.MatchPriority`：

- 优先 `cue.AnchorKey`（V2），其次 `cue.LegacyAnchorKey`（V1），再次 `HighlightId+Part`（即 P1–P3）；
- **不做文本兜底**：无锚点的极旧数据在打点期不自动复用（复用错了没有撤销通道），统一由对账上报后走「重新对齐」；
- 行为与对账器 P1–P3 完全一致，避免两套规则漂移；
- 回归用例见第 8 节第 12 条。

### 5.5 集成点 1：加载（只读）

文件：`LearningAssistant.WinApp/Forms/Pdf/SentenceShadowingForm.cs`

- 构造期 `_alignmentCues` 构建（第 152–159 行）：数量不等时不再清空，改为记录 `_cueCountMismatch = true` 并提示；`CanAlignAudio()` 改判 `_alignmentCues.Count > 0`。
- 列表行源统一为 `_alignmentCues`（见 5.7），文字模式同样如此。
- `LoadSegments()` 之后计算 binding 表：

```
// alignedOnce 不能用 `_segments.Count > 0` 判定（只剩孤儿切片时会误判为“已跳过”）：
// 以 ShadowingProgress.Pages[page].LastSegmentId 为准；旧数据无进度记录时回退 segments.Count > 0。
var progress = _segmentStore.LoadProgress(_pdfPath);
bool alignedOnce = !string.IsNullOrWhiteSpace(
        progress?.Pages.GetValueOrDefault(_pageIndex.ToString())?.LastSegmentId)
    || (progress == null && _segments.Count > 0);

var result = ShadowingCueReconciler.Reconcile(_alignmentCues, _segments,
    s => File.Exists(_segmentStore.ResolvePath(s.SlicePath)),
    alignedOnce: alignedOnce);
_bindings = result.Bindings;
_bindingsDrift = result;   // 保留 Orphans/Duplicates 供修复入口使用
```

- 加载只读：不写盘、不删除 Orphan；仅当 `HasDrift` 时在 `_labelStatus` 提示（见第 6 节）。

### 5.6 集成点 2：保存（对账 + 提示 + 快照）

文件：`SentenceShadowingForm.FinishAlignmentAsync()`（第 973–1072 行）

- **快照**：对账使用即将写盘的**同一份 manifest 实例**，不在 `SaveSegmentsAsync` 前后各读一次文件。
- 在 `SaveSegmentsAsync` 之前对账：

```
var reconciled = ShadowingCueReconciler.Reconcile(_alignmentCues, manifest.Segments,
    s => File.Exists(_segmentStore.ResolvePath(s.SlicePath)), alignedOnce: true);
```

- **不新增、不删除记录**：manifest 原样保存（`created` 已按 Id 合并、时间变化已删旧切片）。
- 保存后 `_segments` 与 `_bindings` 都按 cue 顺序重建，**不再按 `StartMs` 排序**（保证 I4）。
- 状态文案追加：`· N 句无原声 · M 条旧切片未匹配`（N = UnboundCount，M = Orphans.Count）。
- 保持“尽力对齐”语义：对账只上报，不阻断保存。

**写盘原子性**：实施前确认 `ShadowingSegmentStore` 采用“临时文件 + 原子替换”；若不是，需先修（否则播放中崩溃会留下半截 JSON，对账读到脏数据）。

### 5.7 集成点 3：列表渲染（两模式统一）

文件：`SentenceShadowingForm`

| 位置 | 现状 | 改为 |
| --- | --- | --- |
| `ActiveSentences()` | 文字模式用 `_textSentences`；切片模式用 `BuildSegmentSentences()` | **两种模式的行都由 cue 生成**：`ShadowingSentence.Id = cue.AnchorKey`、`Text = cue.Text`；切片模式的 `BuildSegmentSentences` 改为从 `_bindings` 构建 |
| `BuildSegmentPresets()` | 对 `_segments` 逐个取切片 | 对 `_bindings` 取 `Segment.SlicePath`，无则 null（回退 TTS） |
| `FillSentenceList()` | `_segmentMode && i < _segments.Count` 判定 Ready/Failed | 用 `_bindings[i].Status`：Matched/Ambiguous → Ready；MissingSlice → Failed；Unbound → Pending（文字徽标） |
| `SentenceRow` | `Index/PageIndex/Text/State` | 增加 `BindingStatus / UnboundReason`，绘制时区分“原声 / 缺切片 / 已跳过 / 未对齐” |
| `SetCurrentIndex` | 以 `_practice.Sentences` 下标 | 不变；行数 = cue 数，下标即 cue 下标 |
| `SyncSegmentTake` / `LoadSentenceRecordings` | 以 `segment.Id` 为键 | 键解析顺序见 5.8 |

必须同步替换的“按下标取切片”位置（否则列表改按 cue 渲染后会越界）：

| 行号 | 代码 | 改法 |
| --- | --- | --- |
| 394–396 | `for (i < _segments.Count) … _segments[i].Id`（恢复进度） | 按 `_bindings[i].Segment?.Id` |
| 480–486 | `CurrentSegment()` 用 `_practice.Sentences[i].Sentence.Id` 反查 `_segments` | 改读 `_bindings[_currentIndex].Segment`；行 Id 改为 `cue.AnchorKey` 后，按 Id 反查 `_segments` 会永远落空，必须同步改 |
| 1335–1336 | `_segments[row.Index].Takes.Count`（绘制复盘次数） | 按 `_bindings[row.Index].Segment?.Takes` |
| 1377–1379 | `_segmentMode && i < _segments.Count … _segments[i].SlicePath` | 按 `_bindings[i]` |

### 5.8 录音与评分键（降级为“仅文字模式兼容”）

事实约束：切片模式的录音**持久化在 `segment.Takes` 内**（见 `ShadowingSegments.cs`），UI 侧 `_sentenceRecordings` 只是以 `segment.Id` 为键的缓存（`SyncSegmentTake`）。因此**切片模式的键保持 `segment.Id` 不动，不做迁移**——ID 与切片同生死，takes 天然跟随；V2 升级命中旧切片时 `Id` 沿用旧值（`Mark` 现有行为），键与录音均不受影响。

仅需处理文字模式的历史键兼容：

- 读：先按 `cue.AnchorKey` 查录音索引，未命中回退 `cue.SentenceId`（惰性回退，不写回新键）；
- 写：新录音只用 `cue.AnchorKey`；
- 不做“首次保存批量归一”：录音索引与 segments.json 无强一致关系，逐条惰性迁移即可，避免全量备份/回滚的复杂度。

### 5.9 合成（composed）

`_btnCompose` 不再使用 `_segments`，改用 bound 且在 cue 顺序上的切片：

```
var ordered = _bindings.Where(b => b.Segment != null && SliceUsable(b.Segment))
                       .Select(b => b.Segment!)
                       .ToList();
```

**空洞策略（明确下来）**：跳过句在时间轴上不保留空洞，按 cue 顺序**紧邻拼接**，句间插入 0.12s 静音间隔；不提供开关。

### 5.10 修复入口（对账结果可操作）

| 入口 | 位置 | 行为 |
| --- | --- | --- |
| 「重新对齐此句」 | 列表行右键 / 行尾图标 | 仅当 `Unbound / MissingSlice` 可用；起点时间 = 该句前一条已绑定切片的 `EndMs`，无前序则从 0；向 session 传**全量** `existingSegments`，保证旧切片按 ID 复用而不是新建（由用例 15 回归） |
| 「重新对齐缺失句」 | 页脚 | 同上，批量处理 `Unbound / MissingSlice` 的 cue |
| 「清理未匹配切片」 | 页脚（仅 `HasDrift` 时显示） | 本期只清理**当前页**：删除 SlicePath 无引用的切片文件与记录，并同步清理指向它们的 take；跨页引用计数列为后续迭代 |

原则：**没有自动删除**。清理入口显示前必须列出将被删除的文件清单供确认。

### 5.11 加载期漂移的定位信息

`ReconcileResult` 之外的诊断（仅日志/状态栏，不写盘）：

- `OrderViolationCount`：bound 切片的 `StartMs` 与 cue 顺序不单调的条数（只告警，不重排）；
- `AnchorMissCountByStage`：P1/P2/P3/P4 各命中多少条，用于判断是否“锚点整体失效”（P1≈0 且 P2≈0 说明 V2/V1 都没命中，需修复）。

---

## 6. UI 表现与文案

| 场景 | 位置 | 文案 |
| --- | --- | --- |
| 对齐完成、无漂移 | `_labelStatus` | `原声对齐完成：X 句有原声。` |
| 对齐完成、有跳过 | `_labelStatus` | `原声对齐完成：X 句有原声，Y 句已跳过。` |
| 对齐完成、有未匹配旧切片 | `_labelStatus` | 追加 `；M 条旧切片与本页句子不匹配，可清理。` |
| 加载时未对齐完 | `_labelStatus` | `本页尚未完成对齐，N 句还没有原声。` |
| 加载时发现漂移 | `_labelStatus` | `本页有 M 条切片未匹配当前句子，仍按句子顺序显示。` |
| 列表徽标 | `OnSentenceDrawItem` | 原声 = 品牌色；缺切片 = 橙色描边；已跳过 = 灰色；未对齐 = 淡灰虚线 |

原则：**不弹窗、不阻塞**，只提示 + 列表可视化 + 显式修复动作。

---

## 7. 边界与异常

| 情况 | 处理 |
| --- | --- |
| `cues` 为空 | 返回空 bindings，全部 segment 计入 Orphans |
| `segments` 为空 | 全部 Unbound + `NotAlignedYet`，`HasDrift=false` |
| 同一 cue 命中多条记录 | 主候选按 5.3 规则，其余进 Duplicates，状态 Ambiguous |
| 手工打点切片（`HighlightAnchor == null`） | 走 P4 文本匹配；文本也为 null 则只能进 Orphans |
| 同页重复文本删掉其中一条 | 同文本序号平移，可能误配：列入风险，提示“请核对” |
| 页面重新分句且文本边界改变 | V2 也无法匹配，全部 Unbound + Orphans；不自动迁移 |
| V1 切片存在、V2 尚未生成 | 由 P2 命中，复用旧切片（ID 不变，录音可用）；注意 P2 只在坐标未变时成立 |
| 一条高亮覆盖多条同文本句 | V2 组内序号保证锚点唯一（第 8 节用例 14） |
| 切片文件被外部删除 | MissingSlice；行内可“重新对齐此句”重建 |
| 整本模式 | 不参与对账（`_wholeBook` 无单页 segments），行为不变 |
| `File.Exists` 抛异常 | `sliceExists` 内部吞异常返回 false，按 MissingSlice |

---

## 8. 验收用例（可直接落成 xUnit）

文件：`LearningAssistant.Core.Tests/ShadowingCueReconcilerTests.cs`、`ShadowingAnchorKeysTests.cs`

1. `Reconcile_ExactAnchorMatch_ReturnsMatchedInCueOrder`
   两条 cue、两条 segment 且乱序传入 → bindings 顺序等于 cue 顺序。
2. `Reconcile_SkippedCue_ReturnsUnboundNotAlignedPlaceholder`
   3 cue、2 segment（中间句跳过）→ `[Matched, Unbound, Matched]`，`UnboundReason=Skipped`。
3. `Reconcile_EmptySegments_ReturnsAllNotAlignedYet`
   无切片 → 全部 `Unbound` 且 `UnboundReason=NotAlignedYet`。
4. `Reconcile_StageMatching_PreventEarlierCueStealingExactMatch`
   第 1 句仅有文本兜底、第 2 句有精确锚点，且两条 cue 文本相同、只有一条 segment → 该 segment 必须归第 2 句（验证 I3）。
5. `Reconcile_V2Anchor_MatchesAfterCoordinateChange`
   同一高亮坐标变化、文本不变 → V2 仍命中并复用旧切片。
6. `Reconcile_LegacyV1Anchor_MatchesAndReusesSegment`
   只有 V1 锚点的历史切片 → P2 命中，ID 不变。
7. `Reconcile_MissingSliceFile_ReturnsMissingSliceAndKeepsSegment`
   `sliceExists=false` → MissingSlice，Segment 不为 null。
8. `Reconcile_UnmatchedSegment_GoesToOrphansAndIsNotDeleted`
   入参 segments 数量不变，Orphans 含该条。
9. `Reconcile_NeverBindsOneSegmentTwice`
   两条 cue 文本相同且只有一条 segment → 第二条为 Unbound。
10. `Reconcile_IsPermutationInvariant`
    随机打乱 segments → 输出完全一致。
11. `Reconcile_IsIdempotent`
    用结果再跑一次 → 输出不变。
12. `FindExisting_And_Reconciler_AgreeOnP1ToP3`
    同样输入下 `FindExisting` 与 `Reconcile` 的 P1–P3 选择一致（P4 分组分配只在 Reconcile 内）。
13. `AnchorKeys_V2_StableWhenSentenceIdChanges`
    文本相同、`sentence.Id` 变化 → V2 cue 锚点不变。
14. `AnchorKeys_V2_CueAnchorUnique_WhenHighlightCoversSameTextSentences`
    一条高亮覆盖两条文本相同的句子 → 两条 cue 锚点不同（组内序号 0/1）。
15. `Alignment_RestartFromCue_UsesPreviousBoundEndMsAsStart`
    从中间 cue 重对齐 → `startMs` = 前一条已绑定切片的 `EndMs`，无前序时为 0。

---

## 9. 文件改动清单

| # | 文件 | 位置 | 改动 | 风险 |
| --- | --- | --- | --- | --- |
| 1 | `LearningAssistant.Core/Services/Shadowing/ShadowingAnchorKeys.cs` | 新增 | V1/V2 锚点生成 + 合成 | 低 |
| 2 | `LearningAssistant.Core/Models/Shadowing/ShadowingAlignment.cs` | `ShadowingCue`（第 9–30 行） | 增加 `LegacyAnchorKey`（不持久化） | 低 |
| 3 | `LearningAssistant.Core/Services/Shadowing/ShadowingHighlightSource.cs` | `BuildPageCues`（139–193）、`BuildAnchorKey`（198–212） | 改用 V2 + 计算同文本序号与组内序号；保留 V1 生成用于兼容 | **中**（锚点口径变化） |
| 4 | `LearningAssistant.Core/Services/Shadowing/ShadowingCueReconciler.cs` | 新增 | 纯函数对账 + 分阶段匹配 + 字典索引 + `MatchPriority` | 低 |
| 5 | `LearningAssistant.Core/Services/Shadowing/ShadowingAlignmentSession.cs` | `FindExisting`（143–169） | 改为调用 `MatchPriority`（V2→V1→ID），移除文本兜底 | 中（回归第 12 条） |
| 6 | `LearningAssistant.WinApp/Forms/Pdf/SentenceShadowingForm.cs` | 152–159 | 数量不等不再清空 cues，改为提示 | 低 |
| 7 | 同上 | `LoadSegments`（241–259） | 计算 `_bindings` / `_bindingsDrift`；不写盘 | 低 |
| 8 | 同上 | `FinishAlignmentAsync`（973–1072） | 保存前对账；按 cue 顺序重建；快照约束；文案 | 中 |
| 9 | 同上 | `ActiveSentences` / `BuildSegmentPresets` / `BuildSegmentSentences` / `CurrentSegment`（261–313、362–370、480–486） | 两模式行由 cue 生成（`Id = cue.AnchorKey`）；`BuildSegmentSentences` 改从 `_bindings` 构建；`CurrentSegment` 改读 `_bindings` | 中 |
| 10 | 同上 | `FillSentenceList`（1368–1420） | 用 `_bindings[i].Status` 决定徽标 | 低 |
| 11 | 同上 | `SentenceRow` + `OnSentenceDrawItem`（1270–1366） | 增加 BindingStatus / UnboundReason 样式 | 低 |
| 12 | 同上 | `LoadSentenceRecordings`（文字模式，1448 起） | 读：`cue.AnchorKey` → `cue.SentenceId` 回退；写：只用 `cue.AnchorKey`；切片模式键保持 `segment.Id` 不变 | 低 |
| 13 | 同上 | `ComposePageAsync`（1695 起） | 按 cue 顺序 + 紧邻拼接 + 0.12s 间隔 | 低 |
| 14 | 同上 | 页脚 / 行右键 | 新增 5.10 三个修复入口 | 中 |
| 15 | `LearningAssistant.Core/Services/Shadowing/ShadowingSegmentStore.cs` | 写盘 | 校验/补上“临时文件 + 原子替换”；无引用清理 API | 中 |
| 16 | `LearningAssistant.Core.Tests/ShadowingCueReconcilerTests.cs` | 新增 | 第 8 节 1–12、15 条 | 低 |
| 17 | `LearningAssistant.Core.Tests/ShadowingAnchorKeysTests.cs` | 新增 | 第 8 节第 13、14 条 + V1/V2 格式测试 | 低 |

不改动：`ShadowingSegments.cs`（SchemaVersion 仍为 3，不新增持久化字段）、`PdfAudioPlayerBar.cs`、`PdfHiddenFeatures.cs`。

---

## 10. 分阶段实施

| 阶段 | 内容 | 可见变化 | 回滚方式 |
| --- | --- | --- | --- |
| P1 | 锚点 V2 + 双读 + 对账器 + 单测 | 无 UI 变化 | 删除新文件、还原 `BuildPageCues` / `FindExisting` |
| P2 | 加载/保存接入，状态栏提示 + 漂移诊断 | 用户能看到“已跳过 / 未对齐 / 未匹配” | 还原 2 处调用 |
| P3 | 列表统一按 cue 渲染、徽标、合成顺序 | 跳过句不再从列表消失 | 还原第 9/10/11/13 项 |
| P4 | 修复入口 + 清理（引用计数） | 用户可自助修复 | 移除按钮与入口 |

每阶段独立可编译、可回滚，不需要运行时特性开关。

---

## 11. 风险与未决问题

| 类型 | 内容 | 建议 |
| --- | --- | --- |
| 风险 | 同页重复文本删除一条导致 V2 序号平移 | 对账时若 P1 命中率突降且存在重复文本，提示“请核对”；不自动猜 |
| 风险 | 文本被编辑（高亮文本或分句边界变化）时 V2 也无法匹配 | 属不可恢复漂移，保留 Orphans 并提示重新对齐 |
| 风险 | `_segments[i]` / 按 Id 反查的旧写法 | 已知 4 处（394–396、480–486、1335–1336、1377–1379），实施前全局搜索确认 |
| 风险 | V2 升级时若未复用旧切片会生成新 ID，旧 takes 变孤儿 | 对齐路径必须传入全量 `existingSegments` 并沿用旧 ID（`Mark` 现有行为）；由用例 6/12 回归 |
| 风险 | 清理切片误删他页引用 | 本期只清当前页；跨页清理需全量引用计数，列为后续 |
| 未决 1 | V1 兼容读取保留到哪个版本 | 建议保留 2 个迭代，之后加日志统计 P2 命中数再移除 |
| 未决 2 | 整本模式是否接入对账 | 本期不接；整本无 segments，天然跳过 |
| 未决 3 | 修复入口是否支持“只重对齐未匹配句”以外的批量策略 | 本期只做单句 + 批量缺失句 |

---

## 12. 明确不做

- 不恢复 `ShadowingMarkSession` 的入口，也不为其补锚点绑定。
- 不改变 `segments.json` 结构、不写字段级迁移脚本（仅锚点字符串口径升级 + 双读）。
- 不在加载阶段自动删除或重建切片文件。
- 不提供合成空洞开关（固定紧邻拼接 + 0.12s 间隔）。

---

## 13. 本轮修订对照

| 评审意见 | 落地位置 |
| --- | --- |
| 锚点含坐标 → 重建漂移 | 5.1 V2 锚点（ContentKey，不含坐标/句 ID），V1 双读 |
| 贪心逐 cue 降级会抢占精确命中 | 5.3 改为分阶段全局匹配（I3） |
| 只提示、无修复出口 | 5.10 三个修复入口 + 6 节文案 |
| 文本兜底误配重复句 | 5.3 P4 增加“同文本序号一致”约束 |
| 两套列表源 | 5.7 两模式统一按 cue 渲染 |
| 录音键永久双读 | 5.8 一次性归一 + 三级回退 |
| 跳过 / 未对齐混淆 | 5.2 `CueUnboundReason` + 6 节文案 |
| 保存期读两次文件 / 半截 JSON | 5.6 快照约束 + 原子写盘校验 |
| O(n²) 对账 | 5.3 三字典索引 O(n) |
| 缺性质测试 | 第 8 节第 10、11 条（置换不变 / 幂等） |
| 合成空洞未定义 | 5.9 固定紧邻拼接 + 0.12s 间隔 |
| **二次审查（本轮）** | |
| V2 cue 锚点在“一高亮多句同文本”时碰撞 | 5.1 组内序号替代句文本哈希；用例 14 回归 |
| P4“同文本序号”在 segment 上不存在，无法逐对比较 | 5.3 改为文本分组分配；`MatchPriority` 只覆盖 P1–P3 |
| `_practice` 仍以 `segment.Id` 为主键，行源改 cue 后会落空 | 5.7 行 Id 改为 `cue.AnchorKey`；`CurrentSegment` 改读 `_bindings` |
| 录音键归一与 takes 存储冲突 | 5.8 降级为仅文字模式兼容；切片模式键保持 `segment.Id` |
| 「重新对齐此句」缺起点语义 | 5.10 起点 = 前序切片 `EndMs`；传全量 `existingSegments`；用例 15 回归 |
| P2 被误描述为“抗重建” | 5.3/第 7 节措辞改为“兼容未升级数据，坐标未变才成立” |
| `alignedOnce` 用 `_segments.Count > 0` 会误判 | 5.5 改为 `ShadowingProgress.LastSegmentId` 为主、旧数据回退 |
| 清理入口成本被低估 | 5.10 限定本期只清当前页，跨页引用计数列为后续 |
