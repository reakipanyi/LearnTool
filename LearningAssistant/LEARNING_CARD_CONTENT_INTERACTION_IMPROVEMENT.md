# LearningCard 内容交互优化方案

> 评估对象：`LearningForm` 与 `LearningCard` 相关功能
> 核心诉求：在学习英文单词/句子等内容时，能在「发音」「例句」等明细项后挂一个小喇叭（🔊）按钮，单独播放该项内容
> 状态：**仅评估，未改动任何代码**
> 版本：v5（修复播放来源追踪 bug + 复制反馈 + TTS 不可用处理 + 快捷键映射同步）

---

## 一、问题背景

当前 `LearningCard` 用一个 `_contentLabel`（`Label`）把所有明细字段（拼音/音标/释义/例句/翻译…）拼成**单个字符串**显示，字段之间以 `\n` 分隔。

这导致：

1. **内容扁平化**：所有明细变成纯文本，无法区分"这一行是发音""这一行是例句"。
2. **无法挂接交互**：因为是一整块 Label，没办法在某个字段后面单独放一个 🔊 按钮去播放该字段。
3. **发音粒度受限**：现在的发音按钮（`buttonPronounce` / Space）只能整体播放 `MainContent`（标题，即单词本体），并通过 `PronunciationScope`（Original / Explanation / Both）选择是否追加 `Meaning`。**无法单独播放例句、翻译、音标对应内容**。
4. **答题模式同样受限**：`GetDisplayStruct()` 返回的是 `标签:?` 占位串，同样扁平，揭示答案时直接整块替换为 `GetDisplayText()`，缺乏字段级揭示与字段级发音。
5. **播放状态无反馈**：用户点击发音后不知道"正在播"还是"已播完"，也无法中途停止。
6. **无法单独复制**：字段值无法单独复制，需手动选中。

---

## 二、现状分析（关键文件与职责）

### 2.1 控件层

| 文件 | 角色 | 关键点 |
|------|------|--------|
| [Forms/UserControls/LearningCard.cs](file:///e:/Github/LearnTool/LearningAssistant/Forms/UserControls/LearningCard.cs) | 卡片控件（继承 `Panel`） | 内含 `_iconLabel` / `_titleLabel` / `_contentLabel` / `_categoryLabel` / `_accentBar`，通过 `_innerLayout`（3 行 TableLayoutPanel）布局；仅对外暴露 `Title`/`Content`/`Category`/`Icon`/`AccentColor`/`IsSelected` 字符串属性，**没有字段级结构** |
| [Forms/UserControls/LearningContentView.cs](file:///e:/Github/LearnTool/LearningAssistant/Forms/UserControls/LearningContentView.cs) | 内容区容器 | 持有 `_panelContent`，`LearningCard` 被塞进 `PanelContent`；同时保留了旧的 `_labelContent`/`_listBoxDisplay`（目前卡片模式下未使用，**死代码**） |

### 2.2 数据/格式化层

| 文件 | 角色 | 关键点 |
|------|------|--------|
| [Models/Learning/LearningItem.cs](file:///e:/Github/LearnTool/LearningAssistant/Models/Learning/LearningItem.cs) | 学习项聚合根 | 持有结构化字段：`MainContent`、`Meaning`、`Example`、`Pronunciation`、`CharacterFeatures`、`WordFeatures`，**数据本身是结构化的** |
| [Services/Learning/LearningItemFormatter.cs](file:///e:/Github/LearnTool/LearningAssistant/Services/Learning/LearningItemFormatter.cs) | 格式化器 | `FormatDisplayText` / `FormatDisplayStruct`：按 `SubCategory` 走 switch，把各字段拼成 `标签: 值` 的字符串，用 `\n` join —— **结构化数据在此被扁平化**；当前**无单元测试** |
| [Models/Learning/ValueObjects/Pronunciation.cs](file:///e:/Github/LearnTool/LearningAssistant/Models/Learning/ValueObjects/Pronunciation.cs) | 发音值对象 | `Main` / `UkPhonetic` / `UsPhonetic` |
| [Models/Learning/ValueObjects/Meaning.cs](file:///e:/Github/LearnTool/LearningAssistant/Models/Learning/ValueObjects/Meaning.cs) | 释义值对象 | `Content` |
| [Models/Learning/ValueObjects/Example.cs](file:///e:/Github/LearnTool/LearningAssistant/Models/Learning/ValueObjects/Example.cs) | 例句值对象 | `Content` / `Translation` |

### 2.3 表现层

| 文件 | 角色 | 关键点 |
|------|------|--------|
| [Forms/LearningForm.cs](file:///e:/Github/LearnTool/LearningAssistant/Forms/LearningForm.cs) | 主窗体（视图） | `InitializeLearningCard()` 创建卡片并订阅 `Click` → `ContentArea_Click`；`UpdateLearningCard()` / `UpdateDetailState()` 设置 `Title`/`Content`；`PlayPronunciationAsync(text, language)` 调用 `_ttsService.SpeakAsync`；`ButtonPronounce_Click` 触发 `PronounceClicked` 事件；Dispose 中有 `-=` 清理模式（行 3464） |
| [Presenters/LearningFlowHandler.cs](file:///e:/Github/LearnTool/LearningAssistant/Presenters/LearningFlowHandler.cs) | 学习流处理器 | `PlayPronunciationAsync(item, explanation, ct)`：读取 `item.GetMainContent()` 与 `explanation`，根据 `_view.PronunciationScope`（Original/Explanation/Both）决定播放什么；维护 `_pronunciationQueue`（`ConcurrentQueue`）与 `PreloadNextPronunciationAsync`（用 `SpeakToCacheAsync` 预缓存） |
| [Views/ILearningView.cs](file:///e:/Github/LearnTool/LearningAssistant/Views/ILearningView.cs) | 视图接口 | 定义 `PronunciationScope` 枚举（Original / Explanation / Both）、`PronounceClicked` 事件、`PlayPronunciationAsync(text, language)` |

### 2.4 TTS 层

| 文件 | 角色 | 关键点 |
|------|------|--------|
| [Services/TTS/ITTSService.cs](file:///e:/Github/LearnTool/LearningAssistant/Services/TTS/ITTSService.cs) | TTS 接口 | `SpeakAsync` / `SpeakStreamAsync` / `SpeakToCacheAsync` / `StopAsync` / `IsSpeaking` / `Available` —— **接口能力已足够支撑单字段播放**；当前无统一协调层，全局发音（Presenter 队列）与潜在的字段发音会共享同一实例；**无播放状态变化通知** |

### 2.5 主题服务

| 文件 | 角色 | 关键点 |
|------|------|--------|
| [Common/Themes/IThemeService.cs](file:///e:/Github/LearnTool/LearningAssistant/Common/Themes/IThemeService.cs) | 主题接口 | 项目已有主题系统；当前 `LearningCard` 颜色硬编码；`IsAnswer` 行底色需走主题 |

### 2.6 数据流（现状）

```
LearningItem（结构化）
   │
   ├─ GetMainContent() ──► _learningCard.Title       （单词/汉字本体）
   │
   └─ GetDisplayText() ──► LearningItemFormatter
                              │  (switch SubCategory, AddIfNotEmpty, "\n".Join)
                              ▼
                         "词性: n.\n音标: /æpəl/\n释义: 苹果\n例句: ..."
                              │
                              ▼
                         _learningCard.Content（单个 Label.Text）
                              │
                              ▼
                         用户看到一整块文本（无法挂交互）

发音路径（全局）：
   ButtonPronounce / Space ─► PronounceClicked ─► LearningFlowHandler.PlayPronunciationAsync
                                                       │
                                                       ├─ text = item.GetMainContent()  ← 只能是标题
                                                       └─ explanation = Meaning.Content  ← 受 PronunciationScope 控制
                                                       └─ _pronunciationQueue + PreloadNextPronunciationAsync（缓存下一项）
```

---

## 三、根因分析

| # | 根因 | 体现 |
|---|------|------|
| R1 | **`LearningItemFormatter` 把结构化字段压成扁平字符串** | `FormatDisplayText` 返回 `string`，字段边界丢失 |
| R2 | **`LearningCard` 用单个 `Label` 承载所有明细** | 没有"行"概念，无法在某行挂按钮 |
| R3 | **`LearningCard` 对外只接受 `string Content`** | 调用方无法把结构化字段传进来 |
| R4 | **发音入口只有一个、且只认 `MainContent`** | `LearningFlowHandler.PlayPronunciationAsync` 写死 `item.GetMainContent()`，没有"按字段播"的能力 |
| R5 | **答题模式揭示粒度是"整块替换"** | `UpdateDetailState` 在 `GetDisplayStruct()` 与 `GetDisplayText()` 之间二选一切换，无法字段级揭示 |
| R6 | **无统一发音协调层** | `ITTSService` 被 Presenter 队列与视图分别调用时无串行化保证，多源发音会叠加 |
| R7 | **播放状态无反馈** | `ITTSService.IsSpeaking` 是属性但无变化通知，用户无法感知播放进度 |
| R8 | **`LearningContentView` 死代码残留** | `_labelContent`/`_listBoxDisplay` 在卡片模式下未使用 |

注意：**模型层（`LearningItem` + 值对象）本身已经是结构化的**，结构化信息是在格式化/渲染环节丢失的。因此改造重点在"格式化输出 + 卡片渲染 + 发音协调 + 发音入口"四处，**不需要动模型层**。

---

## 四、改进目标

1. **字段级渲染**：卡片明细区按"一行一字段"渲染，每行可独立承载 `[标签] [值] [可选操作按钮]`。
2. **字段级发音**：在「发音/音标」「例句」「例句翻译」等可发音字段后挂 🔊 按钮，点击只播该字段内容；播放中切换为 ⏸ + 高亮背景，再点停止；支持 `Alt+1..Alt+5` 快捷键。
3. **字段级复制**：每个字段后挂 📋 按钮，点击复制该字段值到剪贴板；复制成功有反馈。
4. **发音协调统一**：字段发音与全局发音走同一协调层（`ISpeechCoordinator` 吸收 `_pronunciationQueue`），避免叠加播放；复用已有缓存；**播放来源追踪**确保只有对应行按钮更新状态。
5. **播放状态反馈**：🔊 按钮在播放期间切换为 ⏸/高亮背景，用户可感知播放进度；支持再点停止；TTS 不可用时按钮禁用。
6. **保持 MVP 结构**：字段发音/复制经视图事件 → Presenter 处理，不绕过 Presenter。
7. **保持现有交互**：整卡点击（答题揭示）、悬浮缩放、选中描边、圆角等行为不变；全局 `buttonPronounce`/Space 仍播放默认范围。
8. **键盘可访问**：🔊/📋 按钮可 Tab 聚焦 + 回车触发；`Alt+1..Alt+5` 快捷键对应前 5 个可发音字段；`SetFields` 刷新后映射同步更新。
9. **答题模式视觉层次**：提示字段（词性/音标）与答案字段（释义）视觉区分，揭示前后状态清晰；颜色走主题服务。
10. **全枚举覆盖**：`BuildFields` 覆盖所有 `SubCategoryType` 值，无空白显示。
11. **不破坏模型与持久化**：`LearningItem` 与 JSON 结构不动。

---

## 五、设计方案对比

### 方案 A：最小改动 —— `_contentLabel` 改用 `RichTextBox` + 链接化点击

- 把 `_contentLabel` 换成只读 `RichTextBox`，用 `LinkClicked` 事件区分点击的"字段"。
- 在格式化时给可发音字段加可识别标记（如 `[🔊]`）作为链接文本。

| 优点 | 缺点 |
|------|------|
| 改动小，仅 `LearningCard` + `LearningItemFormatter` | 视觉粗糙（链接下划线/颜色不统一），按钮感弱；定位"哪一字段"依赖文本解析，脆弱 |
| 复用现有 `LinkClicked` 机制 | 富文本与答题模式占位串混用易乱；难以做悬浮态、按下态、键盘聚焦、播放状态反馈、复制按钮、播放来源追踪 |

### 方案 B：字段行控件化 —— 每行一个 `ContentFieldRow`（推荐）

引入一个轻量行控件 `ContentFieldRow`（继承 `Panel`/`UserControl`），内部布局：`[标签 Label] [值 Label] [🔊 Button(可选)] [📋 Button(始终)]`。`LearningCard` 的明细区改为一个垂直容器（`FlowLayoutPanel` 或 `TableLayoutPanel` 动态行），按字段列表逐行添加 `ContentFieldRow`，并对相同字段数做行复用。

- 数据契约改为传递**结构化字段列表**而非字符串。

| 优点 | 缺点 |
|------|------|
| 字段边界清晰，按钮可独立挂事件 | 需新增 `ContentFieldRow` 控件 + 调整 `LearningCard` 明细区容器 |
| 视觉与交互可独立设计（悬浮/按下态/键盘聚焦/播放状态反馈/复制按钮/播放来源追踪） | 需要新的数据载体（字段列表）从 Formatter 传到 Card |
| 易扩展（字段级揭示、收藏等） | |
| 与答题模式字段级揭示/视觉分层天然兼容 | |

### 方案 C：完全自定义绘制 —— 在 `OnPaint` 里画行 + 命中测试

- `LearningCard` 自己 `OnPaint` 画明细，维护行矩形列表，`MouseDown` 做命中测试判断点了哪个 🔊/📋。

| 优点 | 缺点 |
|------|------|
| 性能好，视觉自由度最高 | 实现复杂度高，文本测量/换行/命中测试/无障碍都要自己写 |
| 不增加控件树 | 维护成本高，键盘可访问性极难做（与目标 8 冲突）；播放状态反馈需自绘；播放来源追踪需额外状态管理 |

### 推荐：**方案 B**

理由：在改动量、可维护性、可扩展性、可访问性、播放状态反馈、复制按钮、播放来源追踪之间最平衡；`LearningItem` 已是结构化数据，自然映射成字段行列表；同时为后续"字段级收藏/揭示"留出空间。

---

## 六、推荐方案详细设计（方案 B · v5）

### 6.1 引入结构化字段载体（`record`，砍掉 `Kind` 枚举，加 `IsAnswer`）

新增一个**渲染无关**的字段描述类型（放在 `Services/Learning` 下，与 `LearningItemFormatter` 同目录）。项目已有 `record` 先例（`Models/Learning/LearningContext.cs`），保持一致：

```csharp
/// <summary>明细区一个字段的渲染与发音描述（不可变）。</summary>
public sealed record ContentField(
    string Label,            // "音标"、"例句"…
    string Value,            // 字段值（答题模式未揭示时为 "?"）
    string? SpeakText = null,   // 非空 => 该字段可发音；显示的 Value 与朗读文本可不同（音标显示 /æpəl/，朗读 "apple"）
    string? SpeakLanguage = null, // "en"/"zh"，未指定则按科目推断；例句翻译应强制 "zh"
    bool IsAnswer = false       // 答题模式下作为"答案"的字段（通常为释义/翻译），用于视觉分层
);
```

**关键设计决策**：
- **砍掉 `ContentFieldKind` 枚举**：无分支消费，是否显示 🔊 完全由 `SpeakText != null` 决定。
- **`record` 而非 `sealed class`**：值相等、不可变、`with` 表达式便于答题模式从占位切到真实值。
- **新增 `IsAnswer`**：支持答题模式视觉分层（目标 9），不增加渲染分支复杂度。
- **复制功能**：所有字段都可复制，由行控件统一处理（📋 按钮始终显示），不需要在 `ContentField` 中标记。

### 6.2 改造 `LearningItemFormatter`（补全所有 `SubCategoryType`）

新增返回 `IReadOnlyList<ContentField>` 的方法，原 `FormatDisplayText`/`FormatDisplayStruct` 改为基于此派生（保持向后兼容，最终在 P5 移除）：

```csharp
public static IReadOnlyList<ContentField> BuildFields(LearningItem item, bool revealAnswer)
{
    // 按 SubCategory switch，构造 ContentField 列表
    // revealAnswer=false 时 Value = "?"（替代原 GetDisplayStruct 的 "标签:?"）
    // 可发音字段设置 SpeakText；IsAnswer 标记在释义/翻译字段
    // 覆盖全部 SubCategoryType 枚举值，避免空白显示
}

// 派生（过渡期保留）：
public static string FormatDisplayText(LearningItem item)
    => string.Join("\n", BuildFields(item, true).Select(f => $"{f.Label}: {f.Value}"));
```

**字段映射表（覆盖全部 `SubCategoryType`）**：

| SubCategory | 字段 | SpeakText | SpeakLanguage | IsAnswer |
|-------------|------|-----------|---------------|----------|
| **EnglishWord** | 词性 | — | — | false |
| | 音标 | `item.MainContent` | `en` | false |
| | 英式 | `item.MainContent` | `en` | false |
| | 美式 | `item.MainContent` | `en` | false |
| | 拼读 | — | — | false |
| | 释义 | `item.Meaning.Content` | `zh` | **true** |
| | 词形 | — | — | false |
| | 搭配 | — | — | false |
| | 例句 | `item.Example.Content` | `en` | false |
| | 例句翻译 | `item.Example.Translation` | `zh`（强制） | false |
| **EnglishPhrase** | 音标 | `item.MainContent` | `en` | false |
| | 释义 | `item.Meaning.Content` | `zh` | **true** |
| | 例句 | `item.Example.Content` | `en` | false |
| **EnglishSentence** | 翻译 | `item.Meaning.Content` | `zh` | **true** |
| **ChineseCharacter** | 拼音 | `item.MainContent` | `zh` | false |
| | 释义 | `item.Meaning.Content` | `zh` | **true** |
| | 笔画 | — | — | false |
| | 部首 | — | — | false |
| | 结构 | — | — | false |
| | 组词 | — | — | false |
| | 例句 | `item.Example.Content` | `zh` | false |
| **ChinesePhrase** | 拼音 | `item.MainContent` | `zh` | false |
| | 释义 | `item.Meaning.Content` | `zh` | **true** |
| | 例句 | `item.Example.Content` | `zh` | false |
| **ChineseIdiom** | 拼音 | `item.MainContent` | `zh` | false |
| | 释义 | `item.Meaning.Content` | `zh` | **true** |
| | 例句 | `item.Example.Content` | `zh` | false |
| **ChinesePoem** | 作者 | — | — | false |
| | 朝代 | — | — | false |
| | 内容 | `item.GetExtendedProperty<string>("Content")` | `zh` | **true** |
| **ChineseComprehensive** | 内容 | `item.MainContent` | `zh` | false |
| | 释义 | `item.Meaning.Content` | `zh` | **true** |
| **EnglishComprehensive** | 内容 | `item.MainContent` | `en` | false |
| | 释义 | `item.Meaning.Content` | `zh` | **true** |

### 6.3 新增行控件 `ContentFieldRow`（emoji + FlatStyle + 播放状态反馈 + 复制按钮 + 主题适配 + 播放来源追踪）

路径建议：`Forms/UserControls/ContentFieldRow.cs`

```
答题模式未揭示：
┌──────────────────────────────────────────────────┐
│ [释义]  ?                              📋       │   ← IsAnswer 行：加粗 + 主题浅底
└──────────────────────────────────────────────────┘
揭示后：
┌──────────────────────────────────────────────────┐
│ [音标]  /æpəl/                    🔊        📋   │   ← 普通行：🔊 可见 + 📋 可见
│ [释义]  苹果                      🔊        📋   │   ← IsAnswer 行：加粗 + 主题浅底
│ [例句]  I like apples.            ⏸(高亮)   ✓   │   ← 播放中：🔊→⏸ + 高亮背景；复制成功：📋→✓
└──────────────────────────────────────────────────┘
```

- **布局**：`[标签 Label] [值 Label] [🔊 Button(可选)] [📋 Button(始终)]`，用 `FlowLayoutPanel` 或 `TableLayoutPanel`（3 列：标签占固定宽、值占剩余宽、按钮固定宽）。
- **🔊 按钮**（已确认：emoji + `FlatStyle.Flat`）：
  - 仅当 `SpeakText != null` 时可见。
  - `FlatStyle.Flat`，`TabStop = true`，`Text = "🔊"`，Tooltip "点击播放 / 回车播放（Alt+N）"。
  - **播放中**（已确认：图标切换 + 背景色变化）：`Text = "⏸"` + 背景色高亮（如 `Color.FromArgb(200, 76, 175, 80)`），Tooltip "点击停止"。
  - **播放结束**：恢复 `Text = "🔊"` + 背景色透明。
  - **再点停止**：点击 ⏸ 时触发 `StopRequested` 事件。
  - **TTS 不可用**（v5 新增）：当 `ITTSService.Available == false` 时，按钮 `Enabled = false`，Tooltip 改为"语音服务不可用"。
- **📋 按钮**（已确认：需要字段级复制）：
  - 所有字段都显示，`FlatStyle.Flat`，`TabStop = true`，`Text = "📋"`，Tooltip "复制"。
  - **复制成功反馈**（v5 新增）：点击后 `Text` 临时切换为"✓"，1-2 秒后恢复为"📋"；Tooltip 同步提示"已复制"。
  - 点击时触发 `CopyRequested(ContentField field)` 事件，由 `LearningForm` 调用 `Clipboard.SetText(field.Value)`。
- **IsAnswer 行**：值 `Label` 与 `Value` 加粗，行背景色通过 `IThemeService.GetSecondaryBackgroundColor()` 获取（不走硬编码），支持暗色模式。
- **播放来源追踪**（v5 新增，关键修复）：
  - `ContentFieldRow` 持有当前字段的 `SpeakText` 作为自身标识。
  - 订阅 `ISpeechCoordinator.IsSpeakingChanged` 事件时，比对事件参数中的 `SpeakKey` 与自身 `SpeakText`；只有匹配时才更新按钮状态。
  - 全局发音（`buttonPronounce`/Space）的 `SpeakKey` 设为 `__GLOBAL__` 或空，字段行比对时不匹配则不更新。
- **暴露事件**：
  - `SpeakRequested(ContentField field)` —— 点击 🔊（非播放中）。
  - `StopRequested()` —— 点击 ⏸（播放中）。
  - `CopyRequested(ContentField field)` —— 点击 📋。
- **字体**：由 `LearningCard` 传入（共享 `_fontContent`），不在行内 `new Font()`，防 GDI 泄漏。

### 6.4 改造 `LearningCard`

- 明细区容器：把单一 `_contentLabel` 替换为 `FlowLayoutPanel`（`TopDown`，`WrapContents = false`，`AutoSize = true`）或动态行 `TableLayoutPanel`。
- 新增对外 API（**不再保留 `Content` 字符串属性**）：

```csharp
public void SetFields(IReadOnlyList<ContentField> fields);
public event EventHandler<ContentField>? FieldSpeakRequested;
public event EventHandler? FieldStopRequested;
public event EventHandler<ContentField>? FieldCopyRequested;
```

- **行复用**：`SetFields` 先比较新字段数与现有行数；
  - 数量相同：遍历更新每行 `ContentField` 属性，不重建控件树。
  - 数量不同：差异增删，`SuspendLayout`/`ResumeLayout` 包裹批量操作。
- **布局自适应**：`_innerLayout` 第 3 行由 `Absolute 250F` 改为 `AutoSize`；明细区容器 `AutoSize = true`，卡片本身允许纵向增长；字段过多时明细区 `AutoScroll = true` 兜底，避免 9 字段 + DPI 缩放溢出。
- 整卡 `Click`（答题揭示）行为保持；🔊/📋 按钮 `Click` 处理后**不向上冒泡**（见 §八.1）。
- **播放来源追踪**（v5 新增）：`LearningCard` 持有 `ISpeechCoordinator` 实例，在创建/更新 `ContentFieldRow` 时传递给行控件，由行控件直接订阅 `IsSpeakingChanged`。

### 6.5 改造 `LearningForm`（字段发音/复制走 Presenter）

- `UpdateLearningCard()` / `UpdateDetailState()` 改为：

```csharp
var fields = LearningItemFormatter.BuildFields(_currentItem, revealAnswer: _answerRevealed);
_learningCard.SetFields(fields);
UpdateKeyboardShortcutsMapping(fields); // v5 新增：同步 Alt+1..5 映射
```

- **快捷键映射同步**（v5 新增）：`UpdateKeyboardShortcutsMapping` 根据新字段列表重新计算 `Alt+1..5` 到可发音字段的映射，缓存当前可发音字段列表，供 `ProcessCmdKey` 使用。

```csharp
private void UpdateKeyboardShortcutsMapping(IReadOnlyList<ContentField> fields)
{
    _speakableFields = fields.Where(f => !string.IsNullOrEmpty(f.SpeakText)).Take(5).ToList();
}

// ProcessCmdKey 中：
if (modifiers == Keys.Alt && key >= Keys.D1 && key <= Keys.D5)
{
    var index = key - Keys.D1;
    if (index < _speakableFields.Count)
    {
        FieldSpeakRequested?.Invoke(this, _speakableFields[index]);
        return true;
    }
}
```

- 卡片事件转发到 view 事件：

```csharp
_learningCard.FieldSpeakRequested += (s, f) => FieldSpeakRequested?.Invoke(this, f);
_learningCard.FieldStopRequested += (s, _) => FieldStopRequested?.Invoke(this, EventArgs.Empty);
_learningCard.FieldCopyRequested += (s, f) => FieldCopyRequested?.Invoke(this, f);
```

- `ILearningView` 新增：

```csharp
event EventHandler<ContentField>? FieldSpeakRequested;
event EventHandler? FieldStopRequested;
event EventHandler<ContentField>? FieldCopyRequested;
```

- **复制处理**：`FieldCopyRequested` 在 `LearningForm` 视图内直接处理（`Clipboard.SetText`），无需经 Presenter（纯 UI 操作，无业务逻辑）。

### 6.6 统一发音协调（`ISpeechCoordinator` 吸收 `_pronunciationQueue` + 播放来源追踪）

**问题**：`LearningFlowHandler` 有 `_pronunciationQueue` + `PreloadNextPronunciationAsync`。若字段发音在视图里直接 `SpeakAsync`，会与队列、自动播放、预缓存打架。

**关键 bug**（v5 修复）：`IsSpeakingChanged` 是全局事件，如果用户快速点 A 行 🔊 再点 B 行 🔊，所有行都会收到事件并更新按钮状态，导致状态混乱。必须追踪播放来源。

**方案**：引入 `ISpeechCoordinator`（薄封装于 `ITTSService` 之上，放在 `Services/TTS`），**吸收 `_pronunciationQueue` 作为内部实现**，并增加播放来源追踪：

```csharp
public sealed class SpeakStateChangedEventArgs : EventArgs
{
    public bool IsSpeaking { get; }
    public string? SpeakKey { get; }  // v5 新增：播放来源标识，字段行用 SpeakText 匹配
    
    public SpeakStateChangedEventArgs(bool isSpeaking, string? speakKey)
    {
        IsSpeaking = isSpeaking;
        SpeakKey = speakKey;
    }
}

public interface ISpeechCoordinator
{
    Task SpeakAsync(string text, string language, float? speed = null, CancellationToken ct = default, string? speakKey = null);
    Task StopAsync();
    bool IsSpeaking { get; }
    string? CurrentSpeakKey { get; }  // v5 新增：当前播放的来源标识
    Task PreloadAsync(string text, string language);
    event EventHandler<SpeakStateChangedEventArgs>? SpeakStateChanged;  // v5 重命名并携带来源标识
}
```

- **`SpeechCoordinator` 实现**（已确认：吸收 `_pronunciationQueue`）：
  - 内部维护单一队列（替代 `LearningFlowHandler._pronunciationQueue`），所有发音请求入队串行处理。
  - `SpeakAsync` 入队时记录 `speakKey`，播放开始/结束时触发 `SpeakStateChanged` 事件，携带当前 `speakKey`。
  - `PreloadAsync` 调用 `ITTSService.SpeakToCacheAsync`。
  - `CurrentSpeakKey` 返回当前正在播放的来源标识。
- **播放来源标识规则**（v5 新增）：
  - 字段级发音：`speakKey = field.SpeakText`（确保唯一性，不同字段的 `SpeakText` 通常不同；若相同则用 `field.Label + ":" + field.SpeakText` 组合）。
  - 全局发音（`buttonPronounce`/Space）：`speakKey = "__GLOBAL__"`（特殊标识，字段行比对时不匹配则不更新自身状态）。
- `LearningFlowHandler` 的全局发音与 `FieldSpeakRequested` 触发的字段发音**都走 `ISpeechCoordinator.SpeakAsync`**，内部串行化，保证任意时刻只有一个发音在播。
- **缓存复用**：`EnglishWord` 的"音标/英式/美式"字段 `SpeakText = MainContent`，正是 `PreloadNextPronunciationAsync` 已预缓存的内容，命中即零延迟。
- `LearningFlowHandler` 订阅 `view.FieldSpeakRequested` / `view.FieldStopRequested`：

```csharp
_view.FieldSpeakRequested += async (_, f) =>
{
    var lang = f.SpeakLanguage ?? (_currentSubject == SubjectType.Chinese ? "zh" : "en");
    var speakKey = f.Label + ":" + f.SpeakText;  // v5 新增：唯一标识
    await _speechCoordinator.SpeakAsync(f.SpeakText!, lang, ct: _cts.Token, speakKey: speakKey);
};

_view.FieldStopRequested += async (_, _) =>
{
    await _speechCoordinator.StopAsync();
};
```

- 原 `PlayPronunciationAsync(item, explanation, ct)` 内部从直接调 `_ttsService.SpeakAsync` 改为调 `_speechCoordinator.SpeakAsync`，`speakKey` 设为 `"__GLOBAL__"`；`_pronunciationQueue` 移除，由协调器内部队列替代。

### 6.7 键盘可访问性

- 🔊/📋 按钮 `TabStop = true`，按 Tab 可逐一聚焦，回车/空格触发。
- **`Alt+1..Alt+5`**（已确认：上限 5 个）：在 `LearningForm.ProcessCmdKey` 中为前 5 个可发音字段分配，按 `SetFields` 后的可见 🔊 顺序；**`SetFields` 刷新后映射同步更新**（v5 新增）。
- Tooltip 统一：🔊 为"点击播放 / 回车播放（Alt+N）"；播放中变为"点击停止"；TTS 不可用时变为"语音服务不可用"；📋 为"复制"；复制成功后变为"已复制"。
- 现有 Space（全局主发音）保持不变，不与之冲突。

### 6.8 数据流（改造后）

```
LearningItem（结构化）
   │
   ▼
LearningItemFormatter.BuildFields(item, reveal)  ← 新增，返回 IReadOnlyList<ContentField>
   │
   ▼
LearningCard.SetFields(fields)  ← 行复用，不重建控件树
   │
   ▼
UpdateKeyboardShortcutsMapping(fields)  ← v5 新增：同步 Alt+1..5 映射
   │
   │  逐行生成/更新 ContentFieldRow（订阅 ISpeechCoordinator.SpeakStateChanged）
   ▼
用户看到：[音标]: /æpəl/  🔊 📋    [例句]: I like apples. 🔊 📋    [释义]: 苹果 🔊 📋（加粗+主题浅底）
   │
   ▼ 点击 🔊 / 回车 / Alt+1..Alt+5
ContentFieldRow.SpeakRequested(ContentField)
   │
   ▼
LearningCard.FieldSpeakRequested
   │
   ▼
LearningForm 转发 → ILearningView.FieldSpeakRequested
   │
   ▼
LearningFlowHandler 订阅 → ISpeechCoordinator.SpeakAsync(SpeakText, lang, speakKey: "标签:文本")
   │  （吸收 _pronunciationQueue；串行化；命中 PreloadNext 缓存零延迟）
   ▼
ITTSService.SpeakAsync(...)
   │
   ▼
SpeakStateChanged(isSpeaking: true, speakKey: "标签:文本")
   │
   ▼
ContentFieldRow 比对 speakKey == "标签:文本" → 匹配，更新按钮为 ⏸ + 高亮背景
ContentFieldRow 比对 speakKey != "标签:文本" → 不匹配，保持原样

复制路径（视图内闭环）：
   点击 📋 → ContentFieldRow.CopyRequested → LearningForm.Clipboard.SetText(field.Value) → 按钮临时变为 ✓
```

---

## 七、受影响文件清单

| 文件 | 改动类型 | 说明 |
|------|----------|------|
| `Services/Learning/ContentField.cs`（新增） | 新增 | `record ContentField`（无枚举，含 `IsAnswer`） |
| `Services/Learning/LearningItemFormatter.cs` | 修改 | 新增 `BuildFields`（覆盖全部 `SubCategoryType`）；原字符串方法在过渡期派生自 `BuildFields`，P5 移除 |
| `Forms/UserControls/ContentFieldRow.cs`（新增） | 新增 | 行控件，支持 `IsAnswer` 视觉分层（主题色）、键盘聚焦、播放状态反馈（🔊/⏸+高亮）、复制按钮（📋+成功反馈）、播放来源追踪（比对 `SpeakKey`）、TTS 不可用禁用 |
| `Forms/UserControls/LearningCard.cs` | 修改 | 明细区容器化；`SetFields` 行复用；新增 `FieldSpeakRequested`/`FieldStopRequested`/`FieldCopyRequested`；**删除 `Content` 属性**；明细区 `AutoSize`/`AutoScroll`；传递 `ISpeechCoordinator` 给行控件 |
| `Forms/LearningForm.cs` | 修改 | `UpdateLearningCard`/`UpdateDetailState` 改用 `SetFields`；新增 `UpdateKeyboardShortcutsMapping`；转发 `FieldSpeakRequested`/`FieldStopRequested`/`FieldCopyRequested`；`ProcessCmdKey` 加 `Alt+1..Alt+5`；`FieldCopyRequested` 直接处理 `Clipboard.SetText`；Dispose 中 `-=` 清理新增事件 |
| `Forms/UserControls/LearningContentView.cs` | 修改（P5） | 移除 `_labelContent`/`_listBoxDisplay` 死代码及相关事件订阅 |
| `Views/ILearningView.cs` | 修改 | 新增 `event EventHandler<ContentField>? FieldSpeakRequested`、`event EventHandler? FieldStopRequested`、`event EventHandler<ContentField>? FieldCopyRequested` |
| `Presenters/LearningFlowHandler.cs` | 修改 | 订阅 `FieldSpeakRequested`/`FieldStopRequested`；发音路径切到 `ISpeechCoordinator`；**移除 `_pronunciationQueue`**（由协调器内部队列替代）；传递 `speakKey` 参数 |
| `Services/TTS/SpeakStateChangedEventArgs.cs`（新增） | 新增 | `IsSpeaking` + `SpeakKey` 事件参数 |
| `Services/TTS/ISpeechCoordinator.cs`（新增） | 新增 | 统一发音协调层，串行化 + 缓存复用 + `SpeakStateChanged` 事件（携带来源标识） + `CurrentSpeakKey` |
| `Services/TTS/SpeechCoordinator.cs`（新增） | 新增 | `ISpeechCoordinator` 默认实现，封装 `ITTSService` + 内部队列（吸收 `_pronunciationQueue`） + 播放来源追踪 |
| `LearningAssistant.Tests/LearningItemFormatterTests.cs`（新增） | 新增 | 覆盖每个 `SubCategoryType` 的字段数/顺序/`SpeakText`/`IsAnswer`/`revealAnswer` 两态；加字段顺序断言防视觉回归 |
| `LearningAssistant.Tests/SpeechCoordinatorTests.cs`（新增） | 新增 | 测试串行化（多并发请求串行播放）、`StopAsync` 中断、缓存命中、`SpeakStateChanged` 事件触发（含 `SpeakKey`）、播放来源追踪 |
| `Common/ServiceCollectionExtensions.cs` | 修改 | 注册 `ISpeechCoordinator` |

---

## 八、风险与注意事项

1. **事件冒泡**：`ContentFieldRow` 内 🔊/📋 按钮 `Click` 必须阻止冒泡到 `LearningCard.Click`（答题揭示），否则点按钮会误触发揭示。WinForms `Click` 无 `Handled`，做法：在按钮 `Click` 里直接处理并不再向上 `OnClick`，或在 Card 的 `Click` 里判断 `ActiveControl`/来源控件是否为按钮。**需在 P3 验证**。
2. **TTS 并发**：字段发音与全局发音/自动播放/预缓存可能叠加。由 `ISpeechCoordinator` 单一队列 + `StopAsync` 串行化解决（§6.6）。
3. **GDI/字体资源**：`ContentFieldRow` 字体由 `LearningCard` 传入共享，不在行内 `new Font()`；`LearningCard.Dispose` 已统一释放 `_fontContent` 等。
4. **布局尺寸/DPI**：明细区 `AutoSize` + 必要时 `AutoScroll`，取代固定 250px；需在 100%/125%/150% DPI 下验证 9 字段不溢出。
5. **`GetDisplayStruct` 占位等价性**：`BuildFields(reveal:false)` 的 `Value = "?"` 必须与原 `标签:?` 视觉等价，避免答题模式视觉突变。由 `LearningItemFormatterTests` 守护。
6. **行复用正确性**：`SetFields` 命中复用路径时，必须重置 `IsAnswer` 样式、`SpeakText` 变化时按钮可见性、Tooltip 文本、播放状态；否则旧状态残留。由测试 + 手动验证。
7. **键盘焦点与整卡点击**：🔊/📋 按钮 `TabStop=true` 后，Space 在按钮聚焦时会触发按钮而非全局主发音——需在 `ProcessCmdKey` 里区分"焦点在按钮"与"焦点在卡片"两种 Space 语义，或明确文档约定。
8. **`LearningFlowHandler` 改造范围**：移除 `_pronunciationQueue` 是中等改动，需保证自动播放、`PreloadNextPronunciationAsync` 行为不回归。建议把队列逻辑迁移到 `SpeechCoordinator` 后再删除。
9. **播放状态同步**：`SpeakStateChanged` 事件跨线程（TTS 在后台线程），需在 `ContentFieldRow` 里用 `Invoke`/`BeginInvoke` 切换到 UI 线程更新按钮状态，否则会报 `InvalidOperationException`。**需在 P3 验证**。
10. **事件生命周期**：新增的 `FieldSpeakRequested`/`FieldStopRequested`/`FieldCopyRequested`/`SpeakStateChanged` 必须在 Dispose 中 `-=` 取消订阅，否则会内存泄漏。现有 `LearningForm.Dispose` 已有清理模式（行 3464），需保持一致。**需在 P4 验证**。
11. **主题适配**：`ContentFieldRow` 的 `IsAnswer` 行底色需从 `IThemeService` 获取，而非硬编码。`LearningCard` 通过 `LearningForm` 注入 `IThemeService`（项目已有 `_services.ThemeService`）。**需在 P3 验证暗色模式**。
12. **复制权限**：`Clipboard.SetText` 在部分安全策略下可能失败，需加 try-catch 并提示用户。
13. **播放来源冲突**（v5 新增）：若两个不同字段的 `SpeakText` 完全相同（如音标/英式/美式都读 `MainContent`），`speakKey` 需用 `Label + ":" + SpeakText` 组合确保唯一性。由 `LearningFlowHandler` 在调用时构造唯一 key。**需在 P4 验证**。
14. **全局发音与字段发音互斥**（v5 新增）：全局发音的 `speakKey = "__GLOBAL__"`，字段行比对时不匹配，因此全局播放时字段行 🔊 按钮不会变成 ⏸，符合预期（用户应通过全局按钮控制全局播放）。**需在 P4 验证**。

---

## 九、分阶段实施建议

| 阶段 | 内容 | 产出 | 测试 |
|------|------|------|------|
| P1 | 新增 `ContentField`（record）；`LearningItemFormatter.BuildFields`（覆盖全部 `SubCategoryType`）；原字符串方法派生自 `BuildFields` | 结构化字段输出，旧路径不破坏 | **同步加 `LearningItemFormatterTests`**：覆盖全部 SubCategory + reveal 两态 + 字段顺序断言 |
| P2 | 新增 `SpeakStateChangedEventArgs`；新增 `ISpeechCoordinator` + `SpeechCoordinator`（含内部队列，吸收 `_pronunciationQueue`，含 `SpeakStateChanged` + `SpeakKey` + `CurrentSpeakKey`）；`LearningFlowHandler` 全局发音切到协调器，移除 `_pronunciationQueue`；注册到 DI | 统一协调层就位，字段发音尚未接入 | **同步加 `SpeechCoordinatorTests`**：串行化、`StopAsync` 中断、缓存命中、`SpeakStateChanged`（含 `SpeakKey`）、播放来源追踪；验证全局发音/自动播放/预缓存无回归 |
| P3 | 新增 `ContentFieldRow`（播放状态反馈 + 主题色 + 复制按钮 + 成功反馈 + 播放来源追踪 + TTS 不可用禁用）；改造 `LearningCard`（容器化 + `SetFields` + 行复用 + 三个事件 + 删 `Content` + `AutoSize` + 主题注入 + 传递 `ISpeechCoordinator`） | 卡片支持字段行渲染 | 手动验证事件冒泡、DPI、9 字段布局、播放状态反馈、暗色模式、复制功能、播放来源追踪（快速切换多行播放）、TTS 不可用禁用 |
| P4 | 改造 `LearningForm`（`SetFields` 接线 + `UpdateKeyboardShortcutsMapping` + 转发三个事件 + Dispose 清理 + 复制处理 + `ProcessCmdKey` 加 `Alt+1..Alt+5`）；`ILearningView` 加三个事件；`LearningFlowHandler` 订阅字段发音/停止（传递 `speakKey`）；`ContentFieldRow` 订阅 `SpeakStateChanged` | 字段级 🔊/📋 可用，走 Presenter，播放状态同步，来源追踪正确 | 端到端验证字段发音、复制、键盘、播放状态反馈、与全局发音不叠加、播放来源追踪（快速切换多行）、事件清理、全局/字段发音互斥 |
| P5 | 移除 `FormatDisplayText`/`FormatDisplayStruct` 旧方法及 `LearningCard.Content` 残留引用；移除 `LearningContentView` 中 `_labelContent`/`_listBoxDisplay` 死代码；视觉/答题模式微调 | 收尾，单一代码路径，清理死代码 | 全量回归 |

---

## 十、决策记录（已确认）

| # | 问题 | 决策 | 理由 |
|---|------|------|------|
| 1 | 🔊 按钮视觉形态 | emoji "🔊"/"⏸" + `FlatStyle.Flat` | 与现有 `_iconLabel` 风格一致 |
| 2 | 字段级复制 | 需要，每个字段后加 📋 按钮 | 实用功能，用户可单独复制字段值 |
| 3 | 字段级独立揭示 | 不需要，仍是整卡触发揭示 | 用户明确要求，保持现有交互 |
| 4 | `ISpeechCoordinator` 与 `_pronunciationQueue` | 协调器吸收队列，移除 `_pronunciationQueue` | 单一队列管理，避免双源冲突 |
| 5 | `Alt+N` 快捷键上限 | 前 5 个可发音字段（`Alt+1..Alt+5`） | 避免与现有 `1`/`2`/`3`/`4`/`5` 字母快捷键冲突 |
| 6 | 播放中按钮样式 | 图标切换（🔊→⏸）+ 背景色高亮 | 用户明确要求两者都要，反馈更清晰 |
| 7 | 播放来源追踪 | `SpeakStateChangedEventArgs` 携带 `SpeakKey`，`ContentFieldRow` 比对匹配 | 解决多字段快速切换播放时状态混乱的 bug |
| 8 | 复制成功反馈 | 📋→✓ 临时切换 1-2 秒 | 用户需要知道复制是否成功 |
| 9 | TTS 不可用处理 | 🔊 按钮 `Enabled = false` + Tooltip "语音服务不可用" | 避免点击报错，提升用户体验 |
| 10 | 快捷键映射同步 | `SetFields` 调用后立即更新 `_speakableFields` 缓存 | 确保快捷键始终对应最新字段列表 |

---

## 十一、待办事项

无（全部决策已确认）。

---

## 十二、版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| v1 | 2026-07-11 | 初始方案：字段行控件化、`ContentFieldKind` 枚举、`Content` 退化路径、视图内闭环发音 |
| v2 | 2026-07-11 | 优化：`ISpeechCoordinator` 统一协调、`record` 载体、砍掉枚举、行复用、键盘访问、缓存复用、`IsAnswer` 视觉分层、测试 |
| v3 | 2026-07-11 | 优化：播放状态反馈、主题适配、全枚举覆盖、`SpeechCoordinatorTests`、事件生命周期、死代码清理 |
| v4 | 2026-07-11 | 确认决策：emoji+FlatStyle、复制按钮、协调器吸收队列、`Alt+1..Alt+5`、播放中双状态反馈 |
| v5 | 2026-07-11 | 修复：播放来源追踪（`SpeakStateChangedEventArgs` 携带 `SpeakKey`）；新增：复制成功反馈（📋→✓）、TTS 不可用禁用、快捷键映射同步 |
