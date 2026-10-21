# PDF功能AI能力应用方案

> **Workspace:** e:\Github\LearnTool

---

**User**

文档进行总结、翻译、提取表格、生成思维导图
AI 总结（可整份或指定页面）、AI 翻译（支持逐页翻译和并排对照翻译两种模式，方便原文译文同屏查看）、AI 生成思维导图、AI 解释内容、AI 问答。翻译本身也支持全文、划词、自由翻译等多种模式，覆盖十几种语言

OCR 能力描述较细：支持将扫描件、图片、批量文件转为可编辑或可搜索 PDF；可对扫描图像进行增强和校正（如自动纠偏、画质优化）；支持区域 OCR，只提取页面上选定区域的文字。OCR 语言支持范围较广，并带有智能自动检测。

音频录制是它的核心输入方式之一。你可以从音频开始创建笔记，录音内容会与笔记整合。AI 方面，Note 功能可以从你捕获的所有内容（音频、文本、文件、手写、图片）中自动提炼要点并生成结构化笔记；In-Note Chat 可以针对单条笔记内的所有素材（包括录音）进行提问；Global Chat 则跨所有笔记进行对话。此外还支持将笔记转为闪卡、测验等格式。


你输入的每一行文字都会自动加上与录音对应的时间戳，点击任意一行文字，就能回放写下它时的那段录音。实时转写会记录每一句话并标注说话人。AI 摘要会提炼要点、决定和待办事项，且每一条摘要都链接回背后的原始录音，方便核对上下文。


上述提到的这些功能 哪些能低成本高回报的应用到PDF功能中，请给出建议或方案，不要直接改代码

**Assistant**

## 结论先行

结合现有 PDF 模块的基础来看，真正符合“低成本、高回报”的只有 5 项：**AI 总结/结构化笔记、录音时间戳笔记、高亮/页面生成闪卡测验、逐页双语对照、文档级问答**。其余如表格提取、可搜索 PDF 批量转换、实时转写+说话人分离，成本高且准确率不可控，建议暂缓。

当前可复用的能力已经比较完整：

- 页面文本提取：[IPdfService.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Services/Pdf/IPdfService.cs#L55-L60)
- AI 总结/问答/出题接口：[IAIService.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Services/AI/IAIService.cs#L19-L43)
- 区域 OCR、整页 OCR、图像预处理：[TesseractOcrService.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/TesseractOcrService.cs#L159-L232)
- 翻译（含长文本分段）：[BaiduTranslationService.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Services/Pdf/BaiduTranslationService.cs#L55-L134)
- 录音服务与录音内跳转：[IAudioRecorderService.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Services/Pdf/IAudioRecorderService.cs#L12-L25)、[PdfReaderAudioManager.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfReaderAudioManager.cs#L479-L495)
- 测验引擎、闪卡复习窗口、笔记服务、高亮数据模型，均已存在

## 优先级评估表

| 功能 | 现有可复用基础 | 增量成本 | 回报 | 建议 |
|---|---|---|---|---|
| AI 总结（整份/页码范围） | 页面文本 + SummarizeAsync + 侧栏 | 低-中 | 高 | 第一批 |
| 高亮/批注 → 结构化笔记 | 高亮文本、笔记、页码 + INoteService | 低 | 高 | 第一批 |
| 录音时间戳笔记、点击回放 | 录音服务 + SeekToPosition | 低 | 高（差异化） | 第一批 |
| 页面/高亮 → 闪卡、测验 | QuizEngine + FlashcardReviewForm | 低-中 | 高 | 第一批 |
| 逐页双语对照 | 已有整页 OCR+翻译+翻译Tab | 低-中 | 高 | 第一批（先文本双栏） |
| 文档级 AI 问答（带页码引用） | AskQuestionAsync + 全文搜索 | 中 | 高 | 第二批 |
| AI 思维导图 | AI + TreeView/自绘 | 中 | 中-高 | 第二批 |
| AI 解释（划词/高亮右键） | AIAbilityPanel 快捷动作已有 | 低 | 中 | 顺手补齐 |
| OCR 批量导出文本 | TesseractOcrService 循环 | 低-中 | 中 | 第二批 |
| OCR 语言自动检测 | Tesseract OSD | 低 | 中 | 可选 |
| OCR 自动纠偏/增强 | 现有灰度+二值化 | 中 | 中 | 暂缓 |
| 表格提取 | 无表格结构信息 | 高 | 中（准确率风险大） | 不建议 |
| 批量转可搜索 PDF | 无写文本层能力 | 高 | 中 | 不建议 |
| 实时转写 + 说话人分离 | 本地 System.Speech/WebSpeech | 高 | 中 | 不建议 |
| 全局跨笔记 Chat | 无跨库索引 | 高 | 中 | 暂缓 |

## 五个推荐方案

### 1. AI 总结 + 结构化笔记（最低风险的高回报项）

**做法**

- 优先用 `GetPdfText(pageIndex)` 取文本层；扫描页才回退 OCR。
- 按 3–5 页一个分块做“分块摘要”，再合并成全书摘要，避免整本塞进上下文。
- 每条摘要强制携带 `[P12]` 形式的页码标记，UI 点击直接跳页。
- 按 PDF 哈希 + 修改时间 + 页码缓存摘要，翻页/重开不重复调用 AI。
- 增加“页码范围”选择：当前页 / 当前章节 / 全书。
- 同一份 AI 结果可一键转为结构化笔记：要点、关键概念、待办、疑问，写入现有笔记服务。

**为什么划算**：文本提取、AI 总结、侧栏、笔记服务全部已存在，主要是编排和缓存。

### 2. 录音时间戳笔记（差异化最强）

**做法**

- 边录音边点“记一笔”，记录 `录音Id + CurrentPositionMs + 文本 + 页码`。
- 列表展示“录音 14:32 · 第 3 页 · 笔记内容”，点击调用现有 `SeekToPosition` 回放。
- 录音本身已按 PDF 页码归属，天然可按页分组，不需要改动现有录音文件结构。
- 对这些时间戳笔记再做 AI 摘要，摘要条目同样带时间戳，点击回到原录音位置。

**注意**：不要直接做“实时转写 + 说话人分离”。那需要流式 ASR、说话人分离、转写与时间轴对齐，成本和稳定性风险都远超收益。用“手动时间戳笔记”可以先拿到 80% 的体验价值。

### 3. 高亮/页面 → 闪卡、测验

**做法**

- 数据源三选一：当前页文本、选中页码范围、全部高亮及批注。
- 闪卡正面用高亮文本，背面用批注或 AI 生成的解释。
- 测验直接调用现有 `IQuizEngineService.GenerateQuestionsAsync`，结果进入现有闪卡/测验复习流程。
- 每条卡片保存来源 PDF + 页码，复习时可跳回原文。
- 用文本哈希去重，避免同一高亮反复生成。

**为什么划算**：这是学习类产品最自然的闭环，且测验引擎、闪卡窗口、间隔复习都已存在，不需要新建学习体系。

### 4. 逐页双语对照（先文本双栏，不做页面重排）

当前 [PdfPresenter.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Presenters/Pdf/PdfPresenter.cs#L1636-L1676) 的“整页翻译”有两个可低成本改进的点：

1. 数字版 PDF 也走了整页 OCR，应该先读文本层，扫描页才 OCR。
2. 翻译目标语言固定为中文，而百度服务本身支持 `from/to` 参数，增加目标语言选择即可覆盖十几种语言。

**建议形态**

- 翻译 Tab 内做“左原文 / 右译文”双栏，随翻页自动同步。
- 按页缓存译文，翻回已翻译页不重复请求。
- 支持“逐页翻译”和“整页对照”两个开关。
- 暂不做“把译文重新排版渲染成 PDF 页面”的真并排视图，因为涉及字体、断行、分页、坐标映射，成本和风险都高。

### 5. 文档级 AI 问答（带引用）

**做法**

- 复用现有全文搜索能力，按问题关键词召回 3–5 个相关页。
- 上下文 = 当前页 + 召回页，要求回答必须标注 `[Pxx]`。
- 保留会话上下文，但限制轮数，避免 token 膨胀。
- 无文本层的扫描件先提示 OCR，或在后台先做当前页 OCR。
- 这同时覆盖了产品里的“In-Note Chat”；“Global Chat 跨所有笔记”暂不建议做。

## 可做但要控制范围

- **AI 思维导图**：让 AI 输出严格 JSON 层级树，再用 TreeView 或轻量画布渲染，节点带页码可跳转，支持导出 PNG/Markdown/OPML。不要做自由拖拽编辑器。
- **OCR 批量导出**：把多页/多文件 OCR 结果导出为 md/txt，成本低；但“批量转可搜索 PDF”需要写入不可见文本层并做坐标对齐，不建议现在做。
- **OCR 语言自动检测**：可用 Tesseract OSD 判断文字脚本，失败时沿用现有 `chi_sim+eng` 回退；收益中等，可作为体验优化。
- **AI 解释**：现有 AI 面板已有解释/翻译/总结快捷动作，只需在划词、OCR 结果、高亮右键补入口，并把上下文收窄到所在段落而不是整页。

## 明确不建议现在做

| 功能 | 不建议原因 |
|---|---|
| 表格提取 | PDF 文本层丢失单元格结构，OCR 也需版面分析；AI 纯文本提取准确率不可控 |
| 批量转可搜索 PDF | 需要 PDF 写入、字体嵌入、文本层坐标映射，现有 Pdfium 链路不支持 |
| 实时转写 + 说话人标注 | 需要流式 ASR 与说话人分离，现有本地识别能力不适合长录音 |
| 从音频自动生成结构化笔记 | 强依赖转写质量，属于上一个功能的下游 |
| 全局跨笔记 Chat | 需要跨文档索引与检索体系，属于产品级新能力，不是 PDF 模块优化 |
| 扫描图像自动纠偏 | 现有只有灰度+全局阈值，真正的纠偏需要角度检测/图像变换库，收益有限 |

## 建议的落地顺序

1. **第一批**：AI 总结（含页码引用）、录音时间戳笔记、高亮转闪卡/测验、翻译改为“文本层优先 + 目标语言选择”。
2. **第二批**：逐页双语对照、文档级问答、AI 解释入口补齐。
3. **第三批**：思维导图、OCR 批量导出、语言自动检测。

这样安排的原因：第一批全部建立在已有服务之上，不改渲染链路、不改标注坐标体系、不需要数据库迁移，风险最低且用户感知最强；第二批才涉及新的交互面板和检索逻辑；第三批属于增值功能。

如果你确认这个方向，我可以先把第一批中某一项拆成具体的接口改动、文件清单和分步实施计划（仍然不改代码）。

---

**User**

上述三批和AI的功能先放，不涉及AI的优先完成

**Assistant**

## 范围重新划分

生成式 AI 相关内容全部后置：总结、问答、思维导图、AI 解释、AI 生成闪卡/测验、实时转写、说话人分离、结构化笔记。

纯非 AI 方向主要有三类：**录音标注、OCR 管线、批量导出**。翻译使用百度机器翻译，不是生成式模型，但常被归入“AI 翻译”，因此列为边界项，由你决定是否算进这一批。

## 新的优先级

| 顺序 | 事项 | 当前现状 | 增量成本 | 回报 |
|---|---|---|---|---|
| 1 | 录音时间戳标注 → 点击回放 | **已有约 80% 基础，但未接通** | 低 | 高 |
| 2 | 文本层优先 + OCR/翻译按页缓存 | 当前一律走 OCR | 低 | 高 |
| 3 | OCR 批量导出文本（多页/多文件） | 无 | 低-中 | 中-高 |
| 4 | OCR 自动语言检测接入 | 方法已实现但未暴露 | 低 | 中 |
| 5 | OCR 预处理优化（性能/画质/自动旋转） | 有灰度+二值化，逐像素较慢 | 中 | 中 |
| 6 | 批量转可搜索 PDF | 无 PDF 文本层写入能力 | 高 | 中 |
| 7 | 任意角度自动纠偏 | 无图像算法库 | 高 | 中 |

## 第 1 项：录音时间戳标注回放（最值得先做）

代码里其实已经做了大半：

- 标注模型已有 `AudioTimestampMs` 字段：[PdfAnnotation.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Models/Pdf/PdfAnnotation.cs#L15-L46)
- 画标注时会调用时间戳回调：[AnnotationToolHandler.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/AnnotationToolHandler.cs#L267-L275)
- 管理器间已有时间戳传递链路：[PdfReaderNavigationManager.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfReaderNavigationManager.cs#L185-L236)
- 录音管理器已能取当前录制位置、并支持跳转播放：[PdfReaderAudioManager.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfReaderAudioManager.cs#L479-L499)

目前缺的只有三处：

1. `PdfReaderForm` 没有把 `GetCurrentAudioTimestampMs` 接到录音管理器，所以标注里全是 null。
2. 没有“点击标注 → 跳回录音对应时间”的入口。
3. 同页多次录音时，只有时间戳无法判断属于哪段录音，建议补一个可选的 `AudioRecordingId` 字段（向后兼容，不影响旧数据）。

这项不涉及渲染链路、不改标注坐标体系，风险很低，但用户感知很强。建议第一项就做它。

## 第 2 项：文本层优先 + 按页缓存

现在整页翻译和整页识别通常直接走 OCR：[PdfPresenter.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Presenters/Pdf/PdfPresenter.cs#L1636-L1676)，但数字版 PDF 本身有文本层，`GetPdfText(pageIndex)` 可以直接取。

建议统一成：

- 有文本层 → 直接取文本；
- 文本为空（扫描件）→ 才渲染页面并 OCR；
- OCR 结果和翻译结果按“文件路径 + 修改时间 + 页码 + 语言”缓存，翻回已处理页不再重复请求。

这会同时改善翻译、搜索、导出和后续所有文本型功能，属于基础性优化，但不依赖 AI。

## 第 3 项：OCR 批量导出文本

从产品描述里剥离出最低成本的部分：

- 支持多选文件或页码范围；
- 逐页 OCR，输出 Markdown/TXT，保留页码分隔；
- 有文本层的页直接取文本，跳过 OCR；
- 后台执行，显示进度和取消。

这能覆盖“批量文件转可编辑内容”的核心需求，但明确不做“可搜索 PDF”。

## 第 4 项：OCR 自动语言检测

`PdfOcrService` 里已经有 `RecognizeTextWithAutoLanguageAsync` 和语言判定逻辑，但既没有暴露到接口，也没有被调用：[PdfOcrService.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/PdfOcrService.cs#L58-L129)。

低成本做法是把它加入 `IPdfOcrService`，在首次识别后自动切换 `chi_sim / eng / chi_sim+eng`。注意它是“识别后检测”，首次仍要依赖当前语言，属于渐进式优化。

## 第 5 项：OCR 预处理

现有预处理是灰度 + 全局阈值，并且逐像素 `GetPixel`，在高分辨率页面上较慢：[TesseractOcrService.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/TesseractOcrService.cs#L298-L321)。

可低成本改进：

- 改用 `LockBits` 批量处理像素；
- 增加对比度/去噪；
- 若 `osd.traineddata` 存在，用 Tesseract OSD 做 90°/180°/270° 自动旋转。

任意角度纠偏需要 Hough 变换或 OpenCV，暂不建议。

## 第 6、7 项：暂缓

- **批量转可搜索 PDF**：需要写入不可见文本层、字体嵌入、坐标对齐，现有 Pdfium 链路不具备写 PDF 能力，成本高。
- **任意角度纠偏**：需要额外图像处理库和角度检测，收益不如前几项。
- **表格提取**：即使非 AI 实现也依赖版面分析，准确率不可控，建议直接排除。

## 边界项：翻译

`PdfTranslationService` 已经有带目标语言的重载：[PdfTranslationService.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/PdfTranslationService.cs#L30-L36)，但接口 [IPdfTranslationService.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Services/Pdf/IPdfTranslationService.cs#L16-L29) 没有暴露，UI 也没有目标语言选择。

如果按“非生成式”算进这一批，那么“目标语言选择 + 逐页译文缓存 + 文本双栏对照”也是低成本高回报；如果按你的定义算 AI，就继续保持后置。

## 建议执行顺序

**第 1 项 → 第 2 项 → 第 3 项 → 第 4 项。**

前两项都不需要改数据库结构（第 1 项只加一个向后兼容的可选字段），也不碰 PDF 渲染和标注坐标体系，风险最低。

确认后我可以直接从第 1 项“录音时间戳标注回放”开始实施。

---

**User**

好的，按顺序完成上面的1-4，每步完成后自动提交后继续下一项

**Assistant**

### Relevant Code Snippets
1. e:\Github\LearnTool\LearningAssistant.WinApp\Forms\Pdf\PdfReaderForm.cs:L28-L37
   — `PdfReaderForm` 中 `PdfReaderNavigationManager`、`PdfReaderAudioManager`、`IAudioRecorderService` 字段声明位置；这是接入录音时间戳、音频管理器、导航/标注交互的现有入口字段。

2. e:\Github\LearnTool\LearningAssistant.WinApp\Forms\Pdf\PdfReaderForm.cs:L350-L368
   — `PdfReaderForm` 初始化 `PdfReaderNavigationManager`、绑定撤销栈、标注选择事件和文字标注回调的位置；适合插入“播放对应录音”的标注交互入口。

3. e:\Github\LearnTool\LearningAssistant.WinApp\Forms\Pdf\PdfReaderForm.cs:L430-L438
   — `PdfReaderForm` 从 DI 获取 `IAudioRecorderService` 并创建/初始化 `PdfReaderAudioManager` 的位置；可确认音频管理器实例是否可从表单访问及挂载到哪个 Tab。

4. e:\Github\LearnTool\LearningAssistant.WinApp\Managers\Pdf\PdfReaderAudioManager.cs:L46-L64
   — `PdfReaderAudioManager` 构造函数绑定 `RecordingCompleted`/`PositionChanged`，`Initialize()` 中创建 UI、记录当前 PDF 路径并加载录音；是录音时间戳标注回放功能的核心落点。

5. e:\Github\LearnTool\LearningAssistant.WinApp\Managers\Pdf\PdfReaderAudioManager.cs:L331-L345
   — 录音完成回调 `_recordings.Add(...)`、保存、刷新列表；与“录音时间戳标注”生成后的列表状态更新直接相关。

6. e:\Github\LearnTool\LearningAssistant.WinApp\Managers\Pdf\PdfReaderAudioManager.cs:L409-L490
   — `StartPlayback`、`SeekToPosition` 实现；`SeekToPosition` 会在播放当前录音时跳转到指定毫秒时间戳，是“按时间戳回放”的关键逻辑。

7. e:\Github\LearnTool\LearningAssistant.WinApp\Services\Pdf\NAudioRecorderService.cs:L38-L88
   — `StartRecording` 创建 `AudioRecording`、生成带时间戳的 wav 文件名、记录 `PdfPath/PageIndex` 并启动录音；与录音时间戳标注的源数据生成相关。

8. e:\Github\LearnTool\LearningAssistant.WinApp\Services\Pdf\PdfOcrService.cs:L58-L68
   — `RecognizeTextWithAutoLanguageAsync` 调用 OCR 后根据识别文本自动检测并设置语言；是“OCR 自动语言检测接入”的现有实现位置。

现在处理工具 handler 的回调。

现在处理 NavigationManager 中的事件绑定。

4 项已按顺序完成，每项独立提交，`LearningAssistant.WinApp` 每次构建均通过（`dotnet build ... -c Debug`），未 push。

## 1. 录音时间戳标注回放（commit 待查）

- 模型新增 `AudioRecordingId`，与已有 `AudioTimestampMs` 成对保存：[PdfAnnotation.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Models/Pdf/PdfAnnotation.cs)
- 绘制笔划/形状时捕获当前时间戳与录音 Id：[AnnotationToolHandler.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/AnnotationToolHandler.cs)
- 回调透传并新增“标注写入后”通知：[PdfReaderNavigationManager.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfReaderNavigationManager.cs)
- 持久化签名补充时间戳/录音 Id，文字标注与更新路径保留字段：[PdfPresenter.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Presenters/Pdf/PdfPresenter.cs)
- 「我的录音」页新增“本页时间戳标注”列表，单行显示文字/形状 + 时间，点击定位并播放；翻页、录音完成、标注新增时自动刷新：[PdfReaderAudioManager.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfReaderAudioManager.cs)
- 表单接线：[PdfReaderForm.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs)

旧数据无录音 Id 时回退到同页最近一次录音。

## 2. 文本层优先 + 按页缓存

- 整页翻译先读 `ExtractText` 文本层，为空才回退 OCR：[PdfPresenter.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Presenters/Pdf/PdfPresenter.cs)
- 新增按 `文档路径|页码|OCR语言` 的线程安全缓存（上限 100 页），文本层与 OCR 结果共用；切文档、关闭文档、切 OCR 语言时清空。

## 3. 批量 OCR 导出文本

- 「更多」菜单新增“批量OCR导出文本…”入口：[PdfReaderForm.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs)
- 新增设置/进度窗口（多选 PDF、页码范围、输出目录、md/txt、进度与取消）：[BatchOcrExportDialog.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/Pdf/BatchOcrExportDialog.cs)
- 逐文件串行：文本层直接提取，扫描页用独立 PDF 服务渲染后 OCR；输出 `{文件名}.md/.txt`，重名自动加序号，不覆盖已有文件。

## 4. OCR 自动语言检测

- 接口暴露自动检测重载：[IPdfOcrService.cs](file:///e:/Github/LearnTool/LearningAssistant.Core/Services/Pdf/IPdfOcrService.cs)
- 整页 OCR、选区 OCR/翻译、批量导出统一走检测；识别当前页后切换语言供后续页使用：[PdfTranslationService.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/PdfTranslationService.cs)
- 顺带把区域裁剪/越界校验收敛到 `PdfOcrService`，删除翻译服务中的重复实现。

建议手工验证：录音中新建文字/形状标注→录音页点击跳转；PDF 文本层翻译无 OCR 延迟、来回翻页不重复请求；批量导出含中英混排扫描件时语言自动切换。

---

**User**

commit

**Assistant**

没有可提交的内容：工作区已干净，之前的 4 项已全部入库。

最近提交：
- `43a97fd` OCR 自动语言检测接入
- `80d3b56` 批量 OCR 导出文本
- `1e9e7fd` 离线 OCR 准确率/速度优化（他人提交）
- `eaad892` 文本层优先 + 按页翻译缓存
- `45efeb8` 录音时间戳标注回放

当前 `main` 领先远端 5 个提交，未执行 push。

---

**User**

原有AI功能增强建议以便能更好的扩充上面的功能，列一个实现计划清单，以便分步执行

**Assistant**

## 现状依据

- 现有 AI 能力：`IAIService`（解释/问答/练习/摘要）+ `IAiQuestionService`，`PdfPresenter` 已注入 `_aiService` 并暴露 [GetAiAnswerAsync / GenerateAiContentAsync](file:///e:/Github/LearnTool/LearningAssistant.Core/Presenters/Pdf/PdfPresenter.cs#L784-L800)，但尚无 PDF 专用编排、无缓存、无页码锚点。
- AI 入口目前仅把“原文文本框”当上下文传入 [RaiseAiQuestionAsked](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L2492-L2503)，WebView 面板为通用面板 [AIPanelPopupService.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/AIPanelPopupService.cs)。
- 刚完成的 4 项能力可直接作为 AI 的“数据底座”：文本层优先与按页翻译缓存、页级 OCR 自动语言检测、录音时间戳锚点、批量 OCR 导出。
- 暂无录音转写（STT）能力，录音内容只能通过标注元数据间接利用。

## 设计原则

1. 不新增 AI Provider，全部基于现有 `IAIService` 与 `AiConfig` 多供应商配置。
2. 统一“页码锚点”：AI 输出必须能落到 `pageIndex`，可选落到 `recordingId + timestampMs`。
3. 统一“文本底座”：AI 只读 `PdfTextIndex`，不直接反复 OCR/提取。
4. 统一缓存与预算：按页/按功能缓存 AI 结果，控制上下文长度与调用成本。
5. AI 失败必须降级为普通文本结果，不阻塞阅读、OCR、翻译主流程。

---

## A. 基础层（P0，所有 AI 功能的前置）

- [ ] **A1 文档文本索引 `PdfDocumentTextIndex`**
  - 复用 `ExtractText`，空页回退渲染 + OCR（抽取批量导出里的渲染/OCR 逻辑为公共方法）。
  - 页级模型：页码、文本、来源（文本层/OCR）、检测语言、字符数。
  - 磁盘缓存 `{userData}/pdf_text_index/{文件指纹}.json`，指纹=路径+大小+修改时间；损坏自动重建。
  - 按需构建、可取消、显示进度；供 AI、搜索、总结共用。
  - 验收：二次打开同文档直接命中；修改文件后自动失效重建。

- [ ] **A2 PDF 专用 AI 编排 `IPdfAiService`**
  - 方法：`SummarizePagesAsync`、`AskDocumentAsync`、`ExplainSelectionAsync`、`GenerateOutlineAsync`、`GenerateStudyNotesAsync`、`GenerateFlashcardsAsync`。
  - 统一上下文裁剪（每页限长、总字符预算）、页码标记 `[[p12]]`、结构化输出校验、取消与重试。
  - 内部调用现有 `IAIService`，不写新 Provider。
  - 验收：请求失败/超时返回结构化错误；页面锚点均可解析。

- [ ] **A3 AI 结果缓存 `PdfAiCacheService`**
  - Key = 文件指纹 + 页码集合 + 功能 + 语言 + promptVersion + 模型。
  - 磁盘缓存 + 容量上限 + LRU；与 A1 共用指纹。
  - 验收：关闭重开文档，相同摘要直接命中，不重复计费。

- [ ] **A4 AI 入口上下文与作用域升级**
  - `RaiseAiQuestionAsked` 改为传入：当前页文本、选中文本、页码、文档名、可选前后各 1 页。
  - AI 面板增加“选中 / 本页 / 指定页 / 全文”作用域；答案中的页码可点击跳转。
  - 涉及 [PdfReaderForm.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs)、[AIPanelPopupService.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/AIPanelPopupService.cs)、[AIAbilityPanel.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/UserControls/Ai/AIAbilityPanel.cs)。
  - 验收：问答能引用页码；切换作用域不重复抓取全文。

## B. 页面级 AI（P1，直接扩充翻译/OCR 体验）

- [ ] **B1 整页/选区 AI 总结**
  - 结果面板展示，支持“保存为标注/高亮”，标注带“AI 生成”标记与页码锚点；走 A3 缓存。
  - 验收：重复总结同一页秒出；保存后可在标注列表看到 AI 标记。

- [ ] **B2 多页/全文结构化总结与笔记导出**
  - 分章节 Markdown，复用批量导出的输出目录与重名处理逻辑。
  - 验收：导出文件结构清晰、页锚点完整。

- [ ] **B3 文档问答（轻量检索，不做向量库）**
  - 关键词 + 页码邻近窗口召回相关页，拼上下文问答；答案附引用页码并可跳转。
  - 验收：10 页内定位准确；无相关页时明确提示“未在文档中找到依据”。

- [ ] **B4 选中内容解释升级**
  - 现有 `GetAiAnswerAsync` 增加页码上下文、缓存与“添加到编辑器”；替换当前无上下文裸调用。
  - 验收：解释内容与所在页语境一致，长度可控。

## C. 录音与标注 AI（P2，扩充时间戳回放）

- [ ] **C1 时间戳标注 AI 整理**
  - 输入：本页/全书标注的 `pageIndex + AudioTimestampMs + 文字/形状`（不含音频本身）。
  - 输出：要点、待办、疑问三类清单，每条带页码/时间锚点。
  - 验收：生成结果每项都能定位回原页。

- [ ] **C2 AI 摘要条目回链录音**
  - 摘要条目持久化锚点（`pageIndex/recordingId/timestampMs`），在「我的录音」页新增“AI 摘要”列表，点击跳页并从时间戳播放（复用现有 `SeekToPosition`）。
  - 验收：点击摘要可准确跳页并从对应秒数开始播放。

- [ ] **C3 标注主题聚类与复习清单**
  - 按主题聚合全书标注，导出 Markdown 复习清单。
  - 验收：同一主题跨页标注归并正确。

- [ ] **C4（独立评估，暂不排期）录音转写 → AI 笔记**
  - 需要 STT（本地 Whisper 或云端），成本与依赖较高，建议单独立项，不在本轮范围。

## D. 批量与语言（P3，扩充批量导出与自动检测）

- [ ] **D1 批量 OCR 导出加“AI 智能整理”**
  - 选项：标题层级、去除页眉页脚、段落合并、术语统一；失败回退原始 OCR 文本。
  - 涉及 [BatchOcrExportDialog.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/Pdf/BatchOcrExportDialog.cs) 与导出流水线。
  - 验收：关闭开关时输出与当前完全一致；开启后失败不阻断导出。

- [ ] **D2 语言检测贯通 AI**
  - 页级记录 `detectedLanguage`，AI 提示词与翻译目标自动选择；混合文档按页处理。
  - 验收：中英混排文档逐页提示词语言正确。

- [ ] **D3 AI 翻译润色与术语表（可选）**
  - 术语表缓存复用、译文一致性检查；翻译区改为左右并排 + 同步滚动。
  - 验收：术语前后一致；并排视图滚动同步。

---

## 建议执行顺序

1. **第一批**：A1 → A2 → A3 → A4（一次构建 + 一次提交，形成 AI 底座）
2. **第二批**：B1 → B3 → B4 → B2
3. **第三批**：C1 → C2 → C3
4. **第四批**：D2 → D1 → D3

每批完成后单独构建、单独提交，不 push，保持可回滚。

## 暂缓项（高成本/低回报）

- 向量数据库/RAG 全量索引（先用关键词 + 页窗口）
- 录音实时转写与说话人分离（需要 STT 服务）
- 可搜索 PDF 生成、全书自动批注

## 实施前需要确认的 3 个决策

1. AI 结果展示：继续用现有 WebView 面板，还是新建内嵌结果面板（可复制、可保存标注）？
2. 是否允许把整份文档文本发送到云端 AI（需要明确作用域与隐私提示）？
3. 闪卡/练习生成是否直接接入现有学习卡片与错题体系？

确认后可从 A1 开始按清单逐项执行。

---

