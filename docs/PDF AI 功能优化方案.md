# PDF AI 功能优化方案

> 范围：`LearningAssistant.Core/Services/Pdf`、`LearningAssistant.WinApp/Forms/Pdf`、AI 调用层 `LearningAssistant.WinApp/Services/AI`
> 目标：不新增 AI 供应商、不重做 PDF 渲染链路，把已落地的 PDF AI 从“能用”提升到“稳定、可控、能形成学习闭环”
> 性质：方案与实施清单，本文不改代码。

## 0. 结论先行

PDF AI 的功能面已经比较完整：总结、作用域问答、检索问答、选中解释、大纲、学习笔记、闪卡、标注整理、主题聚类、录音摘要回链都已落地。当前主要短板不在“缺功能”，而在三处底座：

1. **模型调用层按“词语解释”场景写死**：所有请求共用 `max_tokens = 1000`、固定“语言学习助手”system prompt、温度 0.7；PDF 的长文总结、笔记、大纲会被截断，JSON 输出也不稳定。
2. **全文类功能是“单次请求 + 静默截断”**：超过上下文预算后直接停止追加页面，既不告诉用户覆盖了多少页，也没有分块合并；文档超过几十页后，AI 实际只看了前面一部分。
3. **交互仍是“等待光标 + 模态弹窗”**：不能流式输出、不能取消、不能追问、不能重生成；用户只能盯着弹窗等，失败后也没有恢复路径。

建议顺序：**先修 P0 六项底座 → 再补检索质量与学习闭环（P1）→ 最后做思维导图、多模态等增量（P2）**。在 P0 完成前，不建议继续叠加新的 AI 功能。

---

## 1. 现状盘点

| 能力 | 现有实现 | 状态 |
|---|---|---|
| 文档文本索引（文本层优先，扫描页 OCR，按指纹落盘） | [PdfDocumentTextIndex.cs](../LearningAssistant.Core/Services/Pdf/PdfDocumentTextIndex.cs) | 已落地 |
| PDF 专用 AI 编排（11 个方法） | [IPdfAiService.cs](../LearningAssistant.Core/Services/Pdf/IPdfAiService.cs)、[PdfAiService.cs](../LearningAssistant.Core/Services/Pdf/PdfAiService.cs) | 已落地 |
| 作用域上下文（选中 / 本页 / 指定页 / 全文） | [PdfAiContextBuilder.cs](../LearningAssistant.Core/Services/Pdf/PdfAiContextBuilder.cs) | 已落地 |
| 关键词召回问答 | [PdfKeywordRetriever.cs](../LearningAssistant.Core/Services/Pdf/PdfKeywordRetriever.cs) | 已落地 |
| AI 结果磁盘缓存（LRU） | [PdfAiCacheService.cs](../LearningAssistant.Core/Services/Pdf/PdfAiCacheService.cs) | 已落地 |
| AI 入口（工具栏菜单 11 项 + 学习侧栏 4 个按钮） | [PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs)、[PdfStudyPanel.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfStudyPanel.cs) | 已落地 |
| 闪卡 → 间隔重复 + 统一学习项 | [PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs) | 已落地 |
| 学习笔记 → Markdown + 笔记库 | [PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs) | 已落地 |
| 标注整理 / 主题聚类 / 复习清单导出 | [PdfAiService.cs](../LearningAssistant.Core/Services/Pdf/PdfAiService.cs) | 已落地 |
| 录音时间戳摘要回链 | [PdfReaderAudioManager.cs](../LearningAssistant.WinApp/Managers/Pdf/PdfReaderAudioManager.cs) | 已落地 |
| 单元测试 | [PdfAiServiceTests.cs](../LearningAssistant.Core.Tests/Services/Pdf/PdfAiServiceTests.cs)（16 个用例） | 已有基础 |

结论：**数据底座和入口都已具备，优化重点应放在“请求参数、长文策略、交互形态、缓存正确性”四件事上，而不是再加接口。**

---

## 2. 问题诊断

### 2.1 P0：直接影响结果正确性或可用性

#### P0-1 输出被 1000 token 硬截断，提示词角色也不对

证据：

- 所有 OpenAI 兼容供应商的请求体固定 `max_tokens = 1000`、`temperature = 0.7`，system prompt 固定为“专业的语言学习助手”：[OpenAICompatibleAIService.cs](../LearningAssistant.WinApp/Services/AI/OpenAICompatibleAIService.cs#L74-L84)
- PDF AI 通过 `AskQuestionAsync(prompt, string.Empty)` 发请求：[PdfAiService.cs](../LearningAssistant.Core/Services/Pdf/PdfAiService.cs#L676-L683)

影响：

- 全书笔记、大纲、闪卡、标注聚类这类输出很容易超过 1000 token，结果会在中途断掉。
- PDF 总结被当成“解释词语”处理，模型容易输出过度简短的答复。
- 温度 0.7 对 JSON 输出不友好，容易多出解释文字或格式漂移。

#### P0-2 超时链路互相打架，且 UI 无法取消

证据：

- PDF AI 自带 90 秒超时：[PdfAiModels.cs](../LearningAssistant.Core/Models/Pdf/PdfAiModels.cs#L37-L47)
- 但共享 `HttpClient` 的超时来自 `AiConfig.TimeoutSeconds`，默认只有 30 秒：[ServiceCollectionExtensions.cs](../LearningAssistant.WinApp/Common/ServiceCollectionExtensions.cs#L68)、[AiConfig.cs](../LearningAssistant.Core/Models/Config/AiConfig.cs#L44)
- 大部分 PDF AI 入口没有传入 `CancellationToken`，AI 调用阶段只设置 `UseWaitCursor`：[PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L4408-L4418)
- 建索引阶段有进度窗口和取消按钮（[PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L4288-L4318)），但索引完成后的 AI 生成阶段没有取消入口。

影响：

- 90 秒的“优雅超时”实际被 30 秒的 HttpClient 先打断，长文请求更容易失败。
- 用户能取消建索引，却无法取消真正耗时的 AI 生成；索引建完后只能继续等。
- 超时、网络错误、索引取消混在一起，错误提示无法区分。

#### P0-3 全文/多页请求静默截断，没有覆盖率

证据：

- 上下文总预算固定 24000 字符，超出后直接 `break`：[PdfAiModels.cs](../LearningAssistant.Core/Models/Pdf/PdfAiModels.cs#L37-L47)、[PdfAiService.cs](../LearningAssistant.Core/Services/Pdf/PdfAiService.cs#L811-L838)
- 作用域“全文”按页码 0 到 N 顺序取页，当前页不优先：[PdfAiContextBuilder.cs](../LearningAssistant.Core/Services/Pdf/PdfAiContextBuilder.cs#L70-L78)

影响：

- 200 页文档做“全文总结/大纲/问答”时，AI 可能只看到前 20～30 页，但 UI 仍显示“全文”。
- 用户在文档后半部分发起全文问答时，当前页可能根本不在上下文里。
- 相同页码范围的结果会进缓存，之后即使想补全也没有入口。

#### P0-4 缓存键不含真实模型，双层缓存还导致“无法重生成”

证据：

- 缓存键使用 `IAIService.ModelName`：[PdfAiService.cs](../LearningAssistant.Core/Services/Pdf/PdfAiService.cs#L701-L708)
- 但 `ModelName` 是显示名（如 `Doubao`），不是实际请求的 `_endpoint.Model`：[OpenAICompatibleAIService.cs](../LearningAssistant.WinApp/Services/AI/OpenAICompatibleAIService.cs#L33)
- 通用 AI 层还会按“问题 + 上下文”的哈希再缓存 3 天：[AbstractAIService.cs](../LearningAssistant.WinApp/Services/AI/AbstractAIService.cs#L64-L85)、[Constants.cs](../LearningAssistant.Core/Common/Constants.cs#L137)

影响：

- 用户在设置里把 `doubao-pro-32k` 换成 `doubao-pro-128k` 后，PDF AI 仍可能命中旧缓存。
- PDF 缓存清掉后，通用层缓存仍会返回旧答案，用户感觉“重新生成了但结果没变”。
- 结构化输出解析失败的结果也会被写入 PDF 缓存，之后每次都返回坏结果：[PdfAiService.cs](../LearningAssistant.Core/Services/Pdf/PdfAiService.cs#L716-L723)
- 缓存读取是同步读整文件并常驻内存，大条目可能卡 UI：[PdfAiCacheService.cs](../LearningAssistant.Core/Services/Pdf/PdfAiCacheService.cs#L75-L103)

#### P0-5 文本索引的共享构建任务绑定首个调用者

证据：

- `GetOrAdd` 用第一个调用者的 `progress` 和 `CancellationToken` 创建构建任务：[PdfDocumentTextIndex.cs](../LearningAssistant.Core/Services/Pdf/PdfDocumentTextIndex.cs#L119-L121)

影响：

- 第二个调用者取消时，取消不了正在跑的任务；第一个调用者取消时，第二个调用者也会一起收到取消。
- 第二个调用者拿不到进度，UI 会一直空白。
- 构建中途取消后已完成页的 OCR 结果全部丢弃，下次从头再来：[PdfDocumentTextIndex.cs](../LearningAssistant.Core/Services/Pdf/PdfDocumentTextIndex.cs#L162-L244)

#### P0-6 交互形态：模态弹窗 + 等待光标，不能流式/追问/重试

证据：

- 结果显示在只读 `RichTextBox` 的模态窗口：[PdfAiAnswerDialog.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfAiAnswerDialog.cs#L22-L65)
- 全文检索问答用 `InputBox` 收问题：[PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L4479-L4481)
- AI 生成阶段只切换鼠标光标，没有耗时进度、取消按钮，也不在后台执行。

影响：

- 长文请求期间阅读器被模态弹窗/等待光标占用。
- 用户不能追问“这句话什么意思”，只能重新选词再走一遍菜单。
- 失败后只能关掉弹窗重来，没有“重试 / 换作用域 / 先 OCR”的恢复路径。

### 2.2 P1：影响质量、成本与学习闭环

| 编号 | 问题 | 证据 | 影响 |
|---|---|---|---|
| P1-1 | 检索只有“中文二元词 + 命中次数”，没有 IDF、词频归一和短语加权；固定召回 5 页 | [PdfKeywordRetriever.cs](../LearningAssistant.Core/Services/Pdf/PdfKeywordRetriever.cs#L49-L102)、[PdfKeywordRetriever.cs](../LearningAssistant.Core/Services/Pdf/PdfKeywordRetriever.cs#L146-L158) | 长文档、术语密集文档容易召回偏题；常见词权重过高 |
| P1-2 | 结构化输出只校验“根节点是不是 JSON”，没有字段校验、修复重试；坏结果也进缓存 | [PdfAiService.cs](../LearningAssistant.Core/Services/Pdf/PdfAiService.cs#L725-L734) | 大纲/闪卡偶尔缺字段；一旦缓存坏结果，后续一直坏 |
| P1-3 | 闪卡不做去重，也不把页码写进复习项；只写 `Category` / `Subject` | [PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L4699-L4742)、[PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L4748-L4803) | 重复生成会重复入队；复习时回不到原文页；`ReviewItem` 已有 `SourceType` / `SourceRefJson` 未使用 |
| P1-4 | 没有 token、延迟、缓存命中、供应商切换的观测；没有单文档成本上限 | [AIServiceFactory.cs](../LearningAssistant.WinApp/Services/AI/AIServiceFactory.cs#L67-L172) | 长文档 map-reduce 后成本不可见；fallback 可能连续尝试多个供应商 |
| P1-5 | 隐私确认只覆盖“整份文档”，不显示将发送的页数/字符数/是否含 OCR | [PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L4324-L4340) | 用户无法判断发送范围；扫描件 OCR 文本的敏感性没有提示 |
| P1-6 | AI 入口分散：外链 WebView 面板、站内问答弹窗、学习侧栏、AI 菜单四套 | [PdfReaderForm.cs](../LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L6747-L6772)、[AIPanelPopupService.cs](../LearningAssistant.WinApp/Services/AIPanelPopupService.cs) | 同一个“问 AI”有多个入口和不同上下文，用户不知道差异 |
| P1-7 | 索引缓存只按“路径 + 大小 + 修改时间”，OCR 语言/引擎升级不会失效 | [PdfDocumentTextIndex.cs](../LearningAssistant.Core/Services/Pdf/PdfDocumentTextIndex.cs#L46-L56)、[PdfTextIndex.cs](../LearningAssistant.Core/Models/Pdf/PdfTextIndex.cs#L71-L76) | 换 OCR 语言后仍用旧文本；升级识别引擎后不重建 |

### 2.3 P2：暂不做或单独立项

| 方向 | 暂缓原因 |
|---|---|
| 向量库 / 全量 RAG | 需要嵌入模型、索引服务和增量更新；先用 P1 的 BM25 + 页码窗口验证收益 |
| 表格提取 / 可搜索 PDF | 需要版面分析与 PDF 文本层写入，现有渲染链路不具备 |
| 图片/图表多模态问答 | 需要视觉模型与页内坐标映射，成本和准确率都不可控 |
| 跨文档全局 Chat | 属于产品级检索体系，不是 PDF 模块优化 |
| 自由拖拽思维导图编辑器 | 可作为大纲导出后的独立视图，先做只读导出更稳 |

---

## 3. 优化目标（验收口径）

| 指标 | 目标 |
|---|---|
| 输出完整性 | 长文总结/笔记/大纲不再因 1000 token 截断；超预算时使用分块合并，或明确告知覆盖率 |
| 覆盖率透明 | 每次 AI 结果都返回 `覆盖页 / 总页`；未覆盖页有明确提示和继续生成入口 |
| 可取消 | 索引构建、检索、AI 生成三个阶段都能取消；取消后阅读器立即可操作 |
| 首字延迟 | 流式模式下首个 token 在 2 秒内出现（不含首次建索引） |
| 缓存正确性 | 模型、提示词版本、OCR 语言变化时缓存必然失效；“重新生成”能绕过两层缓存 |
| 引用可信度 | 答案中的页码必须来自本次上下文；抽样引用准确率 ≥ 95% |
| 重复成本 | 同一文档同一页范围重复总结，AI 调用次数为 0 |
| 失败恢复 | 单次请求失败在 60 秒内给出可操作提示（重试 / 换范围 / 先 OCR） |

---

## 4. 方案设计

### 4.1 模型调用层：增加“按次请求参数”（P0）

**不改现有 `IAIService` 四个方法的行为**，避免影响单词解释、练习等旧功能；新增一个可选接口，由现有 Provider 实现：

```csharp
public sealed record AiChatOptions
{
    public string? SystemPrompt { get; init; }
    public int MaxTokens { get; init; } = 1000;
    public double Temperature { get; init; } = 0.7;
    public bool JsonMode { get; init; }
    public TimeSpan? Timeout { get; init; }
    public string? ModelOverride { get; init; }
}

public interface IAiChatCompletionService
{
    Task<string> CompleteAsync(string prompt, AiChatOptions options, CancellationToken ct);
    IAsyncEnumerable<string> StreamAsync(string prompt, AiChatOptions options, CancellationToken ct);
    string ModelId { get; }      // 实际 endpoint.Model，用于缓存键
    string ProviderId { get; }
}
```

落地要点：

1. `OpenAICompatibleAIService` 统一接收 `options`，替换写死的 `max_tokens`、`temperature` 和 system prompt；`DoubaoAIService` / `DeepseekAIService` 不再各自特判。
2. `FallbackAIService` 实现同样的流式接口；只对网络错误、429、5xx 做 fallback，401/403/400 直接失败并提示“检查 API Key/模型名”。
3. PDF AI 的统一请求参数建议：

| 功能 | max_tokens | temperature | 输出格式 | system prompt 方向 |
|---|---:|---:|---|---|
| 页面/选中总结 | 1200 | 0.2 | 文本 | 文档摘要助手，只依据给定内容 |
| 作用域问答 | 1500 | 0.3 | 文本 + 引用 | 文档问答助手，缺依据要说“未找到” |
| 选中解释 | 1000 | 0.4 | 文本 | 结合所在页解释，不重复原文 |
| 大纲 | 2500 | 0.1 | JSON | 层级大纲，严格 JSON |
| 学习笔记 | 3500 | 0.2 | Markdown | 结构化笔记，页码锚点 |
| 闪卡 | 2500 | 0.1 | JSON 数组 | 每卡一个知识点 |
| 标注整理 | 2000 | 0.2 | Markdown | 要点/待办/疑问三类 |
| 摘要条目 | 1500 | 0.1 | JSON 数组 | 3–8 条，带时间锚点 |
| 主题聚类 | 2500 | 0.1 | JSON 数组 | 3–8 个主题，不重复 |

4. **超时统一**：`HttpClient` 保留 120 秒兜底；PDF AI 用每次请求的 `CancellationTokenSource(TimeSpan.FromSeconds(90))`；删除 `WaitAsync` 与 HttpClient 30 秒的双重截断。其他 AI 功能仍走各自默认值。
5. **流式**：OpenAI 兼容接口统一解析 SSE（`stream: true`），返回 `IAsyncEnumerable<string>`；不支持流式的供应商自动退化为一次性返回。JSON 类请求仍用非流式，便于完整解析。
6. **模型身份**：`IAIService` 增加 `ModelId`（实际模型名），PDF AI 缓存键改用 `ProviderId + ModelId`；同时在结果里回传“本次使用的模型/供应商”。
7. **重试预算**：`FallbackAIService` 增加“总截止时间 + 连续失败熔断”，避免 6 个供应商 × 3 次重试导致几分钟无响应。

### 4.2 长文档理解：分块 map-reduce + 覆盖率（P0）

1. **阈值判断**：上下文总字符 ≤ `MaxTotalChars`（默认 24000）时维持单次请求；超过时自动走分块。
2. **分块**：按 8–10 页或 6000 字符切成 chunk，优先保证页边界完整；每块生成“块摘要 + 页码锚点”，结果以 `feature = chunk-summary` 单独缓存。
3. **归并**：最终请求只放块摘要（不再放原文），生成全书总结/大纲/笔记；块摘要总长仍超预算时做第二层归并。
4. **覆盖率模型**：扩展 `PdfAiResponse`：

```csharp
public int TotalPages { get; init; }
public IReadOnlyList<int> CoveredPages { get; init; }
public IReadOnlyList<int> MissingPages { get; init; }
public bool IsPartial { get; init; }
public string? CoverageWarning { get; init; }
```

5. **UI 展示**：结果窗口/侧栏顶部显示“已阅读 87/120 页”；有未覆盖页时提供“继续生成剩余页”，不要静默当作全文。
6. **作用域修复**：`AskScopedAsync` 的全文范围改为“当前页优先 + 检索召回 + 剩余页按顺序”，避免当前页被预算挤掉。
7. **结构化输出**：
   - 大纲/闪卡/聚类使用 JSON 格式约束；
   - 解析失败先做一次“修复重试”（把原始输出和错误原因发回模型，要求只返回合法 JSON）；
   - 仍然失败则不写缓存，返回“格式异常，可重试”，避免坏结果长期驻留；
   - 页码字段校验范围，并校验引用页是否在本次 `CoveredPages` 内。

### 4.3 检索增强（P1）

不做向量库，先把关键词检索做扎实：

1. **BM25**：按页计算词频、页长归一和 IDF；中文继续用二元词，英文用单词；常见词加权降低。
2. **短语与结构加权**：问题完整短语命中加分；短行/疑似标题行加分；当前页与前后页小幅加分。
3. **召回参数可调**：`topK` 从 5 提到 5–8，邻页扩展可配置；召回结果按 `fingerprint + 归一化问题` 缓存，翻回同一问题不重复计算。
4. **可验证引用**：问答改为结构化输出：

```json
{
  "answer": "...",
  "citations": [{ "page": 12, "quote": "原文中的一小段" }]
}
```

   解析时用归一化文本校验 `quote` 是否真的出现在对应页；校验失败就把该条标为“未验证”，而不是当作可信引用。
5. **可选查询改写**：设置里提供“问题改写”开关，用小 token 预算把口语问题改写成检索词；默认关闭，避免额外延迟。
6. **答案兜底**：召回为空时维持现有“未在文档中找到依据”，不调用 AI。

### 4.4 交互层：内嵌可流式的 AI 侧栏（P0/P1）

1. **新形态**：新增 `PdfAiSidebarPanel`，停靠阅读器右侧，替代“模态弹窗 + 等待光标”为主路径；原 `PdfAiAnswerDialog` 保留为兼容入口，后续再删。
2. **能力**：
   - 输入框直接提问，不再用 `InputBox`；
   - 作用域切换：选中 / 本页 / 指定页 / 全文；
   - 流式追加输出，生成中可“停止”；
   - 结果底部操作：复制、重生成、保存为标注、加入笔记、导出；
   - 引用页码做成可点击 chip，点击跳页并高亮；
   - 同一文档保留会话历史，支持追问，历史随文档切换。
3. **状态机**：`建立索引 → 检索召回 → 生成中 → 完成/失败/已取消`，每一步都有文案和取消按钮；失败时给“重试 / 换作用域 / 先 OCR”三个动作。
4. **展示**：Markdown 基础渲染（标题、列表、代码块、页码 chip）；JSON 结果提供“原文/格式化”切换；纯文本兜底。
5. **入口统一**：工具栏 AI 按钮、右键菜单、学习侧栏的 AI 按钮统一打开同一个侧栏，并带上预设动作（总结本页、解释选中、生成大纲等）；命令面板补齐所有 AI 动作。
6. **结果元信息**：显示本次模型、作用域、覆盖页数、是否命中缓存、耗时；让用户知道“这次答案是怎么来的”。

### 4.5 学习闭环（P1）

1. **闪卡质量**：
   - 每块最多生成 20 张，避免输出截断；
   - 按“归一化正面 + 页码”去重，再进入预览；
   - 预览可编辑正面/背面/页码，勾选后批量导入；
   - 复习项写入 `SourceType = "PdfAiFlashcard"`，`SourceRefJson` 记录 `pdfPath/pageIndex`，复习时可回原文；
   - 统一学习项与复习项只写一次，失败可重试，不重复计数。
2. **测验入口**：新增“用本页/选中内容生成测验”，复用 `IQuizEngineService`；调用前先走 4.1 的请求参数，否则同样受 1000 token 限制。题目写入现有测验/错题流程，并保存来源页。
3. **笔记与大纲**：
   - 导出文件头增加元信息：模型、覆盖页、提示词版本、生成时间；
   - 大纲除文本外提供只读树形视图，节点可跳页；
   - 思维导图导出放到 P2（PNG/Markdown/OPML），不做自由拖拽编辑器。
4. **标注与录音**：
   - 标注整理结果支持“保存为笔记”，并保留页码/时间锚点；
   - AI 摘要条目支持重新生成与删除，继续复用现有录音回链；
   - 主题聚类增加“按主题跳到对应标注”的入口，而不只是导出 Markdown。
5. **反馈**：结果窗口增加“有用/无用 + 可选纠正”，按 `fingerprint + promptVersion + feature` 落盘；只用于评测和提示词迭代，不做自动训练。

### 4.6 索引可靠性（P0/P1）

1. **取消隔离**：共享构建任务使用独立的内部 `CancellationTokenSource`，每个调用者用自己的令牌 `WithCancellation` 等待；调用者取消只停止等待，不杀共享任务。
2. **进度广播**：构建任务内部维护订阅者列表，把进度推给所有等待者；不要让第二个调用者拿不到进度。
3. **断点续建**：每完成 20 页写一次部分索引（`IsComplete = false`），下次从第一个空页继续；完成后写最终文件并原子替换。
4. **失效条件**：索引版本号、OCR 语言、OCR 引擎版本、PDF 指纹任一变化都重建；`Invalidate` 同时清理部分索引。
5. **并发**：Tesseract 当前有全局引擎锁（[TesseractOcrService.cs](../LearningAssistant.WinApp/Services/Pdf/TesseractOcrService.cs#L15)），不要直接并行 OCR；要做并行需先改成引擎池，单独立项。

### 4.7 成本、可观测与评测（P1）

1. **提示词目录**：所有 PDF AI 提示词集中到 `PdfAiPromptCatalog`，每个功能一个版本号；改提示词必须递增版本，缓存自然失效。
2. **日志字段**：功能、供应商、模型、页数、字符数、耗时、是否命中缓存、token 用量（供应商返回时）、错误类型；默认不记录正文。
3. **预算**：请求前估算字符/token；提供单文档每日预算（可配置，例如 5 万 token），超出时提示并优先命中缓存。
4. **评测集**：准备 10–20 份固定 PDF（文本版、扫描版、中英混排、超长、无文本层），断言：
   - 覆盖率字段完整；
   - 引用页码都在上下文内；
   - JSON 结果符合 schema；
   - 重复请求缓存命中；
   - 取消后 3 秒内 UI 可操作。
5. **缓存管理**：设置页显示缓存大小/条目数，支持“清理当前文档缓存”“清理全部 AI 缓存”；重生成时显式跳过两层缓存。
6. **隐私**：发送前显示“将发送 N 页、约 M 字符、包含 OCR 文本、使用某供应商”，支持“仅本次 / 本会话 / 始终允许”；整份文档仍默认需要确认。

---

## 5. 实施计划

### 5.1 第一批（P0，先修底座）

| 序号 | 任务 | 主要改动 | 验收 | 预估 |
|---|---|---|---|---|
| P0-1 | PDF 专用请求参数与提示词 profile | 新增 `AiChatOptions` / `IAiChatCompletionService`；Provider 去掉写死的 1000/0.7/system prompt；`PdfAiService` 按功能传参 | 长文笔记不再截断；JSON 输出稳定；旧 AI 功能行为不变 | 0.5–1 人日 |
| P0-2 | 超时与取消统一 | HttpClient 120s 兜底；每次 PDF 请求 90s CTS；所有 UI 入口传 `CancellationToken`；结果窗口/侧栏加“停止” | 30s 不再提前截断；取消后 3 秒内可操作 | 0.5–1 人日 |
| P0-3 | 缓存身份与“重新生成” | 缓存键改 `ProviderId + ModelId + promptVersion`；结构化失败不写缓存；增加 bypass 参数与清理入口 | 换模型后不命中旧缓存；重生成结果变化 | 0.5 人日 |
| P0-4 | 长文档分块与覆盖率 | `PdfAiResponse` 增加覆盖字段；全文档走 map-reduce；块摘要单独缓存；当前页优先 | 200 页文档覆盖 100% 或有明确未覆盖提示 | 1.5–2 人日 |
| P0-5 | 索引取消隔离与断点续建 | 共享任务内部 CTS；进度广播；每 20 页落部分索引；失效条件补充 OCR 语言/版本 | 两个入口同时建索引互不取消；中途取消后重开可续建 | 1 人日 |
| P0-6 | 内嵌流式 AI 侧栏 MVP | 新增 `PdfAiSidebarPanel`；流式输出、输入框、作用域、停止、复制/重生成、引用 chip；旧弹窗保留兼容 | 生成过程实时可见；可追问；旧入口仍可用 | 2–3 人日 |

### 5.2 第二批（P1，质量与闭环）

| 序号 | 任务 | 验收 |
|---|---|---|
| P1-1 | BM25 + 短语/结构加权 + 引用校验 | 长文档召回更准；引用页码可验证；召回为空不调用 AI |
| P1-2 | 闪卡去重、可编辑预览、来源回链 | 重复生成不重复入队；复习项可跳回原文页 |
| P1-3 | 测验入口（本页/选中 → 现有测验） | 题目进入现有测验流程，来源页保留 |
| P1-4 | 观测、预算、评测集、缓存管理 | 能看到 token/延迟/命中；超预算有提示；评测可重复跑 |
| P1-5 | 隐私预览与缓存清理 UI | 发送前可见范围；用户能清理当前文档缓存 |
| P1-6 | AI 入口统一与会话历史 | 菜单/侧栏/命令面板都落到同一侧栏；同文档历史可追溯 |

### 5.3 第三批（P2，增量）

- 大纲树形视图与思维导图导出（PNG/Markdown/OPML）。
- 表格提取、图片多模态问答、可搜索 PDF（单独立项评估）。
- 向量检索 / 跨文档问答（先用 P1 检索验证收益）。
- 本地模型或离线 AI（取决于产品对隐私与成本的要求）。

---

## 6. 测试与验收

### 6.1 单元测试

在现有 [PdfAiServiceTests.cs](../LearningAssistant.Core.Tests/Services/Pdf/PdfAiServiceTests.cs) 基础上补充：

- 请求参数：不同功能使用不同 `max_tokens` / temperature / system prompt；
- 缓存键：模型、提示词版本、OCR 语言变化时 Key 不同；
- 结构化失败不写缓存；修复重试成功后可写缓存；
- 长文档：超过预算时进入分块，覆盖率字段正确；
- 当前页优先：全文作用域中当前页一定被包含（或明确提示未包含）；
- 引用校验：上下文外页码被过滤，未匹配 quote 标为未验证；
- 索引：两个调用者并发、其中一个取消，不影响另一个；断点续建后页数与指纹正确；
- 闪卡：重复正面去重，`SourceRefJson` 写入正确。

### 6.2 手工测试矩阵

| 文档类型 | 重点验证 |
|---|---|
| 200 页文本版 PDF | 全文总结覆盖率、缓存命中、取消、翻页后追问 |
| 扫描版 PDF（无文本层） | 首次建索引进度、OCR 失败页提示、断点续建 |
| 中英混排 PDF | 语言提示、逐页回答语言、引用页码 |
| 10 页以内小文档 | 单次请求路径不被 map-reduce 误触发、首字延迟 |
| 加密/损坏 PDF | 失败提示、不写坏缓存、索引不残留 |
| 未配置 API Key | 快速失败、不连续 fallback、提示明确 |

### 6.3 反向验证

- 手动制造一次 AI 超时，确认“停止”可用、错误可读、缓存未写入。
- 手动改模型名，确认旧缓存不命中。
- 手动在索引构建中取消，再打开同一文档，确认能续建而不是从头开始。

---

## 7. 风险与取舍

| 风险 | 应对 |
|---|---|
| 流式与 JSON 模式在不同供应商兼容性不一致 | JSON 请求不流式；流式失败自动退化为一次性返回 |
| map-reduce 增加调用次数和成本 | 设置阈值；块摘要缓存；UI 显示覆盖率和预计调用数 |
| 修改超时影响其他 AI 功能 | 新参数只对 PDF AI 生效，旧功能保留默认值 |
| 内嵌侧栏改动较大 | 先做 MVP，保留旧弹窗；按入口逐步切换 |
| 索引部分文件格式变更 | 提升索引版本号，旧缓存自动失效；部分文件单独命名 |
| fallback 熔断误伤 | 只对连续失败启用短熔断，成功后立即恢复 |

---

## 8. 需要拍板的 3 个决策

1. **交互形态**：是否接受“内嵌可流式侧栏”作为主入口，旧弹窗只做兼容？（推荐：接受）
2. **全文上传默认策略**：默认只发当前页/选中，全文必须显式确认；后台建索引只在本地做，不上传。（推荐：按此执行）
3. **成本上限**：是否启用单文档每日 token 预算与自动 map-reduce？默认阈值与上限建议做成设置项。（推荐：启用，默认 5 万 token/日/文档，可调）

确认后建议从 **P0-1 请求参数与提示词 profile** 开始，按 5.1 的顺序逐项实施；每项独立构建、独立提交，保持可回滚。
