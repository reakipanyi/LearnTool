# PDF 渲染链路优化方案（P0）

> 范围：仅覆盖 P0 渲染性能、内存与翻页竞态。不涉及搜索体验、标注矢量化和窗体拆分（见文末「后续项」）。
> 状态：方案设计，未修改任何业务代码。

## 1. 目标

| 维度 | 现状 | 目标 |
| --- | --- | --- |
| 翻页主观延迟 | 一次翻页经历 2 次 PNG 编码 + 2 次 PNG 解码 | 编码/解码次数降为 0，翻页 P95 降低 40% 以上 |
| 内存峰值 | 渲染缓存固定 30 张，理论峰值约 430MB；400% 缩放单张可达 200MB+ | 缓存硬预算 ≤ 256MB；单张渲染硬上限，400% 不 OOM |
| 快速翻页 | 旧渲染结果可能后到并覆盖新页面 | 页码与画面始终一致 |
| 夜间模式 | 每次切换清空缓存并重新光栅化 | 缓存不分昼夜，切换只重绘显示层 |
| 关闭窗口 | 后台预渲染可能与信号量释放竞态 | Dispose 时取消并等待后台任务，无 `ObjectDisposedException` |

## 2. 现状与证据

### 2.1 每页 4 次图像编解码（主瓶颈）

```
PdfDocument.Render
  → Bitmap                       PdfiumPdfService.RenderPage
  → PNG 编码 (1)
  → byte[]
  → PNG 解码 (2)                 PdfRenderer.RenderPageAsync
  → （夜间模式时再解码+编码）
  → PNG 编码 (3)
  → byte[]
  → PNG 解码 (4)                 PdfReaderForm.DisplayImage
  → Bitmap → PictureBox 绘制
```

- [PdfiumPdfService.cs L78-L109](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/PdfiumPdfService.cs#L78-L109)：渲染后 `BitmapToBytes`
- [PdfRenderer.cs L99-L135](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/PdfRenderer.cs#L99-L135)：`BytesToBitmap` → `BitmapToBytes`
- [PdfReaderForm.cs L1653-L1655](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L1653-L1655)：`new Bitmap(new MemoryStream(imageData))`
- 缩放路径额外多一轮：[PdfZoomController.cs L68-L89](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfZoomController.cs#L68-L89) 中 `bytes → Bitmap → DisplayImage(Bitmap) → PNG → bytes → Bitmap`，合计 3 次编码 + 3 次解码。

按 1600×2240 ARGB 估算，单次 PNG 编码/解码约 15–40ms，翻页 CPU 中相当比例消耗在编解码而非光栅化。

### 2.2 缩放渲染尺寸无上限

[PdfZoomController.cs L73-L75](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfZoomController.cs#L73-L75)：

```csharp
int targetW = (int)(_form.PictureBoxPdf.ClientSize.Width * _zoomLevel / 100.0);
int targetH = (int)(_form.PictureBoxPdf.ClientSize.Height * _zoomLevel / 100.0);
```

`ClientSize 1600×2400` 在 400% 时渲染 6400×9600，单张约 245MB，并且该尺寸会作为缓存 key 进入 `_renderCache`。`PdfPresenter.CalculateRenderSize` 有 1600×2240 上限，但缩放路径绕过了 Presenter，因此没有上限保护。

### 2.3 缓存无预算且按「尺寸 × 夜间模式」重复存储

[PdfRenderer.cs L19-L21](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/PdfRenderer.cs#L19-L21) 固定 30 条，key 见 L153-L156，包含 `renderWidth/renderHeight/_isNightMode`：

- 每级缩放都会产生一组全新缓存条目；
- 切换夜间模式会 `ClearCache()`（L90-L97），已预渲染的页面全部作废；
- 缓存条目为深拷贝位图，按 1600×2240×4B ≈ 14MB/张，30 张约 430MB。

### 2.4 渲染无取消、无代次校验

- [PdfPresenter.cs L244-L261](file:///e:/Github/LearnTool/LearningAssistant.Core/Presenters/Pdf/PdfPresenter.cs#L244-L261)：`RenderPage` 先更新页码，再 await 渲染并显示，无取消。
- [PdfRenderer.cs L203](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/PdfRenderer.cs#L203)：`_renderSemaphore.WaitAsync()` 不带 token，排队中的旧请求无法取消。
- 快速连按「下一页」时，信号量允许 2 个并发渲染，第 1 页可能晚于第 2 页完成并覆盖画面，出现「页码显示第 5 页、画面是第 3 页」。

### 2.5 Dispose 与后台预渲染竞态

[PdfRenderer.cs L250-L322](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/PdfRenderer.cs#L250-L322) 的 `SmartPreRenderAsync` 是 fire-and-forget，而 [Dispose L528-L538](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Pdf/PdfRenderer.cs#L528-L538) 直接释放两个 `SemaphoreSlim`，后台任务若仍在 `WaitAsync` 会抛 `ObjectDisposedException`。

## 3. 目标设计

### 3.1 新数据流

```
PdfDocument.Render
  → Bitmap                              （不编码）
  → 缓存 LRU（白天原图、尺寸量化、字节预算）
  → 深拷贝 Bitmap
  → PdfPresenter（代次号校验 + CancellationToken）
  → IPdfView.DisplayImage(Bitmap)       （直接接管，不编解码）
  → Paint：白天直接绘制 / 夜间 ColorMatrix 反色
```

### 3.2 关键设计决策

| 决策 | 方案 | 理由 |
| --- | --- | --- |
| 位图所有权 | 渲染器返回**独立深拷贝**，视图接管并负责释放 | 保持现有缓存语义，避免缓存位图被视图释放 |
| 尺寸量化 | 渲染尺寸按 1.25 倍档位向上取整，再裁剪到硬上限 | 避免缩放 1px 变化导致缓存全部失效 |
| 缓存预算 | 按 `Bitmap.Width*Height*4` 累计，超 256MB 淘汰最久未用 | 内存可预测，避免大文档爆内存 |
| 夜间模式 | 缓存只存白天原图，反色放在 `Paint` | 切换夜间模式零渲染、零缓存清空 |
| 渲染取消 | 每次翻页/缩放取消上一次的 CTS；await 后校验代次号 | 快速翻页不会错页，旧渲染结果直接丢弃 |
| 导出/打印 | 保留 `IPdfService.RenderPage`（byte[]） | `ExportService` 需要字节流嵌 Excel，不影响导出链路 |

## 4. 改动清单

### 4.1 接口层（Core）

**`LearningAssistant.Core/Services/Pdf/IPdfService.cs`**

- 新增：`Bitmap? RenderPageBitmap(int pageIndex, int width, int height);`
- 保留：`byte[]? RenderPage(...)`，注释标注「用于导出，热路径请用 RenderPageBitmap」。
- Core 已引用 `System.Drawing.Common 10.0.8`，可直接使用 `Bitmap`。

**`LearningAssistant.Core/Services/Pdf/IPdfRenderer.cs`**

- `Task<byte[]?> RenderPageAsync(int pageIndex, int width, int height)` 改为
  `Task<Bitmap?> RenderPageAsync(int pageIndex, int width, int height, CancellationToken cancellationToken = default)`。
- `ApplyNightMode(byte[])` 保留但标记为导出/兼容用途；新增 `Bitmap ApplyNightMode(Bitmap source)` 或在视图层实现。

**`LearningAssistant.Core/Views/Pdf/IPdfView.cs`**

- `void DisplayImage(byte[] imageData)` → `void DisplayImage(Bitmap image);`
- `void SetSecondPageImage(byte[]? imageData)` → `void SetSecondPageImage(Bitmap? image);`
- 所有权约定写入注释：视图接管传入位图，调用方不得再释放。

**`LearningAssistant.WinApp/Managers/Pdf/IPdfReaderFormAccess.cs`**

- `void DisplayImage(Bitmap bmp)` 保留签名，实现改为直接转交，不再编码 PNG。

### 4.2 PdfiumPdfService

**`LearningAssistant.WinApp/Services/Pdf/PdfiumPdfService.cs`**

- 实现 `RenderPageBitmap`：

```csharp
public Bitmap? RenderPageBitmap(int pageIndex, int width, int height)
{
    lock (_lockObj)
    {
        if (_pdf == null || pageIndex < 0 || pageIndex >= _pdf.PageCount) return null;
        using var img = _pdf.Render(pageIndex, width, height, 96, 96, PdfRenderFlags.Annotations);
        if (img == null) return null;
        // 必须复制：PdfiumViewer 返回的 Image 生命周期与本次调用绑定，
        // 直接返回会在 using 结束（或文档卸载）后变成已释放对象。
        // 这一次内存拷贝远低于 PNG 编码/解码成本，是安全且划算的。
        return new Bitmap(img);
    }
}
```

- `RenderPage` 改为：`using var bmp = RenderPageBitmap(...); return BitmapToBytes(bmp);`，保证导出行为不变。
- 禁止把 `_pdf.Render` 的返回值原样传出（不复制），否则文档卸载后会出现「位图已释放」崩溃，见风险 R2。

### 4.3 PdfRenderer

**`LearningAssistant.WinApp/Services/Pdf/PdfRenderer.cs`**

1. 缓存结构替换为预算制 LRU：
   - `CacheEntry { Bitmap Bitmap; long Bytes; }`
   - `Dictionary<string, LinkedListNode<CacheEntry>> _cache` + `LinkedList<CacheEntry> _lru`
   - `long _cacheBytes`，常量 `MaxCacheBytes = 256L * 1024 * 1024`
   - `AddToCache(key, bmp)`：写入前累计字节，超预算从 LRU 头部淘汰并 `Dispose`。
2. key 去掉 `_isNightMode`：`$"{_currentFilePath}_{pageIndex}_{w}_{h}"`。
3. 尺寸量化：
   - 新增 `private static (int w, int h) QuantizeRenderSize(int w, int h)`；
   - 以 1.25 倍档位向上量化（100%→125%→156%…），再按页宽高比裁剪；
   - 常量 `MaxRenderWidth = 3200`、`MaxRenderHeight = 4500`，超限时等比缩小。
   - 量化逻辑建议放在 Core 的新类 `PdfRenderSizePolicy`，便于单测。
4. 取消支持：
   - `RenderPageAsync(..., CancellationToken ct)`；
   - `await _renderSemaphore.WaitAsync(ct)`；
   - 渲染完成后、写缓存前检查 `ct.IsCancellationRequested`，已取消则 `Dispose` 结果并返回 `null`；
   - `RenderPageToBitmap` 内部在 Pdfium 渲染前无法中断（同步 API），但可在渲染返回后立即丢弃。
5. `SetNightMode` 不再 `ClearCache()`，只更新标志；`RenderPageAsync` 不再调用 `ApplyNightMode`（移到视图层）。
6. 预渲染：
   - 顺序固定为 `+1, +2, -1, -2`，删除 `Random`（L273-L274）；
   - 预渲染任务登记到 `_preRenderTasks`（`ConcurrentDictionary<long, Task>`），Dispose 时 `Cancel()` → `Task.WhenAll(...).Wait(1000)` → 再释放信号量。
7. 删除未使用的 `_pageTexts` 字段（L17）。
8. 缩略图缓存加简单上限（如 300 张），超限随机淘汰最早插入项（可放本批或 P1）。

### 4.4 PdfPresenter

**`LearningAssistant.Core/Presenters/Pdf/PdfPresenter.cs`**

1. `RenderPageAsync` 返回类型改为 `Task<Bitmap?>`，透传 `CancellationToken`。
2. 新增字段：

```csharp
private CancellationTokenSource? _pageRenderCts;
private int _renderGeneration;
```

3. `RenderAndDisplayCurrentPageAsync`：

```csharp
var cts = new CancellationTokenSource();
Interlocked.Exchange(ref _pageRenderCts, cts)?.Cancel();
int generation = Interlocked.Increment(ref _renderGeneration);
var firstTask = _pdfRenderer.RenderPageAsync(currentIndex, w, h, cts.Token);
...
var image = await firstTask;
if (generation != Volatile.Read(ref _renderGeneration) || cts.IsCancellationRequested)
{
    image?.Dispose();          // 丢弃过期结果
    return;
}
_view?.DisplayImage(image);  // 视图接管
```

- 双页第二页同样带上 `cts.Token`，显示前做同样校验。
- 删除 L299-L312 重复的 `SetSecondPageImage(null)` 分支。
- 切换文件（`OnFileLoaded`）时也要 `Interlocked.Increment`，避免旧文件渲染结果覆盖新文件首屏。
- `Dispose` 中取消并释放 `_pageRenderCts`。

4. 新增 `public async Task RenderPageAtZoomAsync(int pageIndex, int zoomPercent)`：
   - 由 Presenter 统一计算尺寸（`CalculateRenderSize` 或缩放档位），走同一条代次校验逻辑；
   - 供 `PdfZoomController` 调用，替代其直接使用 `RenderPageAsync`。

### 4.5 PdfReaderForm

**`LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs`**

1. `DisplayImage(byte[] imageData)` → `DisplayImage(Bitmap image)`：
   - 直接接管 `image`（不再 `new Bitmap(MemoryStream)`）；
   - 现有 `SafeDisposeImage(ref _currentPageImage)`、旋转、异常回退逻辑保持不变；
   - 异常路径需明确：失败时释放传入位图，避免泄漏。
2. `SetSecondPageImage(Bitmap? image)`：接管并释放旧 `_secondPageImage`；`null` 时清空。
3. `IPdfReaderFormAccess.DisplayImage(Bitmap bmp)`：删除 `MemoryStream + PNG` 中转，直接调用主实现。
4. 夜间模式：
   - 移除 `PdfRenderer.ApplyNightMode` 调用后，在 `DrawPageImage` 中按 `_nightModeManager.IsNightMode` 使用已有的 `DrawInvertedImage`；
   - 双页 `DrawDualPageLayout` 两页都要走同一反色分支；
   - `PictureBoxPdf_Paint` 的 `ClearBackground` 已按夜间色填充，无需改动；
   - 确保导出、打印仍使用原色位图。
5. 反色必须只保留一处：当前渲染器已反色、`Paint` 未调用 `DrawInvertedImage`；迁移后改为仅显示层反色，二者不得同时启用，避免「反转两次回到原色」。

### 4.6 PdfZoomController

**`LearningAssistant.WinApp/Managers/Pdf/PdfZoomController.cs`**

- 删除 `RenderPageAtZoomAsync` 内部对 `_form.Presenter.RenderPageAsync` 的直接调用和 `BeginInvoke + new Bitmap(MemoryStream)`。
- 改为 `await _form.Presenter.RenderPageAtZoomAsync(page, _zoomLevel);`
- 增加 180ms 去抖：连续滚轮/拖动滑块只触发最后一次渲染；去抖期间可用当前位图做 GDI 拉伸即时反馈（可选）。
- `Zoom`/`ZoomByMouseWheel`/`ResetZoom` 统一走去抖入口。

## 5. 兼容与风险

| 风险 | 影响 | 处理 |
| --- | --- | --- |
| 接口签名变更 | 编译期即可发现，无运行时兼容问题 | 一次性改完 5 个实现/调用点 |
| 位图所有权 | 视图接管后若调用方再释放会 `ArgumentException` | 接口注释明确 + 代码审查 |
| `RenderPageBitmap` 的 Pdfium 返回值 | `img as Bitmap` 可能为 null | 回退到 `new Bitmap(img)` |
| 夜间模式移到视图层 | 缩略图/导出仍为原色（符合预期） | 明确说明，不改变导出 |
| 取消渲染 | Pdfium 同步渲染无法中断，只能丢弃结果 | 主要收益是「不显示过期结果」，CPU 收益次要 |
| 缓存预算过小 | 大页面 PDF 频繁淘汰，翻页回退 | 256MB 起步，实测后调；按页面积动态计算上限 |
| 图片模式 | 同样走 Bitmap 返回 | 复用 `RenderPageToBitmap` 现有分支 |

## 6. 验证方案

### 6.1 功能回归

1. 单页模式：连续翻 100 页、首页/末页跳转、页码输入跳转，画面与页码一致。
2. 双页模式：翻页步进 1 页、第二页显示与标注/高亮位置正确。
3. 缩放：50%–400% 全部档位、滚轮连续缩放、锁视图后缩放禁用。
4. 夜间模式：切换后两页都变暗、无「反色两次变回白色」、切换不触发全量重新渲染。
5. 旋转 90/180/270 后标注坐标仍正确。
6. 图片模式（扫描版/文件夹）翻页与缩放正常。
7. 导出高亮 Excel（含图片）内容正确；打印输出为原色原内容。
8. 打开大文件后立即关闭窗口，无异常日志、无卡死。

### 6.2 性能与内存测量

| 指标 | 方法 | 通过标准 |
| --- | --- | --- |
| 翻页延迟 | Stopwatch 包裹 `RenderPage` 到 `DisplayImage` 完成，采样 100 次取 P50/P95 | P95 相比基线下降 ≥ 40% |
| 缩放响应 | 100%→200%→400% 各测 10 次 | 400% 首次渲染 < 800ms，无 OOM |
| 工作集 | `Process.WorkingSet64` 在翻 200 页后强制 GC 采样 | 稳定，无随页数线性增长 |
| 缓存占用 | 渲染器内部计数暴露为日志 | ≤ 256MB |
| 错页率 | 快速连按下一页 20 次，脚本比对页码与画面 | 0 次不一致 |

### 6.3 自动化测试

- 在 Core 新增 `PdfRenderSizePolicyTests`：档位量化、上限裁剪、极端宽高比。
- 在 Core 新增 `LruBitmapCacheTests`：预算淘汰顺序、字节计数、Dispose 调用（用可控大小的假位图或抽象计数）。
- 现有 `LearningAssistant.Core.Tests/Services/Pdf` 可扩展。

## 7. 分阶段落地建议

| 阶段 | 内容 | 风险 | 可独立验证 |
| --- | --- | --- | --- |
| Phase 0 | 绘制/撤销安全守卫：标注修订号（R5）、绘制中挂起换图（R4/R8）、清空标注不再整页重渲染（R7） | 低（不改渲染管线） | 绘制中缩放/翻页、撤销后立即操作 |
| Phase 1 | 代次号 + CancellationToken（P0-4）、尺寸量化与上限（P0-2） | 低-中（R9） | 快速翻页、400% 缩放 |
| Phase 2 | Bitmap 直传，去掉编解码（P0-1） | 中（接口签名、R1/R2/R6） | 翻页延迟、绘制回归、导出回归 |
| Phase 3 | 缓存预算 LRU（P0-3）、夜间模式原子迁移（R3/R12）、Dispose 竞态修复（R10） | 中 | 内存、夜间切换、关窗 |

Phase 0 必须先于其他阶段完成，这是进行渲染提速的前置条件（见 8.4）。Phase 1 不改任何接口签名，可作为第一批独立提交；Phase 2 建议单独提交以便回滚。

## 8. 风险评估（绘制 / 撤销 / 标注专章）

### 8.1 总体结论

本方案**不修改**标注数据模型、坐标归一化规则、撤销栈结构（`_unifiedUndoStack`、`_strokeUndoStack`、`_undoStack`），因此撤销逻辑本身不会被破坏。真正的风险集中在四个交接面：

1. **位图所有权**：页面位图从「视图自己解码持有」变为「渲染器移交」，释放责任变化。
2. **绘制与换图的时序**：绘制直接写 `AnnotationBitmap`，而 `DisplayImage` 会清理该位图。
3. **异步标注层加载**：加载结果只校验页码/路径，不校验标注版本，可能覆盖撤销结果。
4. **夜间模式反色位置迁移**：缓存、Paint、导出三处必须同时正确。

其中 3 项为阻断级（不处理不能进入 Phase 2/3），2 项为高风险。

### 8.2 风险清单

| 编号 | 风险 | 触发场景 | 对绘制/撤销的影响 | 等级 | 缓解措施 | 验证方式 |
| --- | --- | --- | --- | --- | --- | --- |
| R1 | 位图所有权违约 | 缓存位图被直接返回；调用方在 `DisplayImage` 后仍释放同一 Bitmap；异常路径重复释放 | 标注层按 `CurrentPageImage.Width/Height` 建图，位图失效会导致标注层尺寸错误、`ObjectDisposedException`、后续页黑图 | 阻断 | 唯一所有权规则：渲染器只返回**深拷贝**，视图接管并负责释放，Presenter 仅在未交给视图时释放；`CreateDeepCopy` 路径不得改为共享；异常路径用 `try/finally` 明确唯一释放点 | 连续翻 200 页 + 缩放 20 次，标注位置正确、无异常日志 |
| R2 | `RenderPageBitmap` 返回已释放位图 | 直接 `return img as Bitmap` 且 `img` 处于 `using` 作用域；或文档 `Unload()` 后仍绘制 | 页面显示为已释放对象，Paint 抛异常，整个阅读器白屏 | 阻断（方案示例已修正） | 必须 `return new Bitmap(img)`；禁止把 Pdfium 返回值原样传出 | 打开 PDF 后立即关闭/重开另一文件，页面正常 |
| R3 | 夜间模式迁移不原子 | 只改缓存 key 不改渲染反色；或只移除渲染反色不改 Paint；或 Paint 对整个画布反色 | 夜间模式失效 / 双重反色变回白色 / 标注与高亮颜色被反色（黄变蓝、红变青） | 阻断 | 三处必须同一提交完成：①渲染器不再反色 ②缓存 key 去掉夜间标志且不 `ClearCache` ③`DrawPageImage` 仅对**页面位图**使用 `DrawInvertedImage`，标注/高亮在其后绘制；导出与打印不反色 | 夜间画一笔→退出夜间，笔迹颜色不变；导出标注图片为原色；双页两页均变暗 |
| R4 | 绘制中被新位图打断 | 按住画笔绘制时，缩放渲染 / 预渲染 / 刷新渲染返回，`DisplayImage` 调用 `CleanupAnnotationBitmap()` 将位图置空；`FinalizeStroke` 见 `bmp == null` 直接丢弃 | **当前笔画丢失且不写入服务**，用户看到画到一半消失、撤销也无记录 | 高 | 增加 `IsDrawing` 守卫：绘制期间不应用新页面位图，缓存为 `_pendingImage`，`MouseUp` 后再应用；或换图前强制完成当前笔画 | 按住画笔同时滚轮缩放、按下一页，笔画完整保留 |
| R5 | 异步标注层覆盖撤销结果 | `DisplayImage` 后 `Task.Run(LoadAnnotationsAsync)` 读到撤销前的标注；用户此时撤销，旧任务 `BeginInvoke` 应用旧位图（当前只校验页码/路径） | 被撤销的笔画重新出现，用户感知「撤销无效」 | 高 | 增加标注修订号（页面 + revision），`AddStroke/RemoveStroke/Undo/Clear` 时递增；`ApplyLoadedAnnotationBitmap` 前校验 revision；撤销时取消在途加载 | 撤销后 1 秒内不操作，笔画不得复现；连续「画 3 笔→撤销 2 次→快速翻页」20 轮 |
| R6 | 第二页位图与后台任务竞态 | `SafeReplaceImage` 释放旧位图时，`UpdateSecondHighlightLayerAsync` / `LoadSecondAnnotationsAsync` 正在后台读 `SecondPageImage` 或持有旧引用 | 第二页高亮/标注错位或丢失；日志出现 `ObjectDisposedException` | 中 | 后台任务只使用调用时捕获的 `imgW/imgH`，不在 `BeginInvoke` 内再次读取 `SecondPageImage`；应用前校验页码、路径、尺寸三者一致 | 双页模式快速翻页 20 次，右页标注位置始终正确 |
| R7 | 刷新类整页渲染被代次号丢弃 | 清空全部标注（[PdfReaderForm.cs L3244-L3258](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs#L3244-L3258)、[PdfReaderHighlightManager.cs L650-L657](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfReaderHighlightManager.cs#L650-L657)）依赖 `RenderAndDisplayCurrentPageAsync()` 重建图层；随后立即翻页/缩放会取消该渲染 | 清空后图层可能停留在清空前状态，看起来「没清掉」 | 中 | 这两处不依赖整页重渲染，改为 `CleanupAnnotationBitmap() + LoadAnnotationsForCurrentPage() + UpdateHighlightLayer() + Invalidate()`；或新增不受代次取消影响的 `ForceRefreshLayers()` | 清空后立即连按下一页再返回，原页保持清空 |
| R8 | 缩放去抖与选择拖拽冲突 | 拖拽调整标注/文字尺寸时，去抖后的缩放渲染落地，`CurrentPageImage` 像素尺寸变化，拖拽起点与 `SelectedTextOriginalBounds` 仍按旧尺寸计算 | 标注位置跳变、调整错目标 | 中 | 复用 R4 的「绘制/拖拽期间不换图」守卫；换图后按当前选择重新计算 bounds；去抖期间保留旧图只做 GDI 拉伸 | 选中标注→拖动角手柄同时滚轮缩放，位置不跳变（或明确取消拖拽） |
| R9 | CTS 取消后立即 Dispose | `Exchange(...)?.Cancel(); oldCts.Dispose();` 时旧任务正在 `SemaphoreSlim.WaitAsync(token)` 注册回调 | 偶发 `ObjectDisposedException`，翻页中断 | 中 | 只 `Cancel()`，由持有该 CTS 的渲染任务在 `finally` 中 `Dispose()`；不跨线程立即释放 | 连续快速翻页 100 次无异常 |
| R10 | Dispose 阻塞 UI | `Task.WhenAll(...).Wait(1000)` 在 UI 线程等待预渲染；Pdfium 同步渲染无法中断 | 关窗卡顿最多 1 秒，严重时假死 | 低-中 | 超时后不等待，用 `_disposed` 标志让后台任务自行退出；信号量延迟释放（GC/最终清理） | 大文件打开后立即关窗，响应 < 300ms 且无异常 |
| R11 | 尺寸量化改变笔迹/字号观感 | 不同量化档位（如 100% 与 125%）下，线宽与文字字号的像素换算略有差异 | 笔迹看似变粗/变细，文字标注大小轻微跳变 | 低 | 量化步长不超过 1.25 倍；同一缩放级别复用同一量化结果；验收对比 100/125/150/200% | 同一标注在各级缩放下来回切换，粗细与位置稳定 |
| R12 | 夜间模式影响导出 | 若把夜间反色写进位图本身而非 Paint 阶段 | `ExportAnnotationsAsImage` 导出的图片变暗，打印异常 | 低 | 反色只发生在 Paint、只作用于页面位图；导出/打印始终用原色位图 | 夜间模式下导出标注 PNG，背景为白色 |

### 8.3 影响面确认（已核对）

- `IPdfView` 仅 [PdfReaderForm.cs](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Forms/Pdf/PdfReaderForm.cs) 一个实现，接口签名变更不会波及其他窗体。
- `IPdfRenderer.RenderPageAsync` 的调用点只有 [PdfPresenter.cs L220-L224、L283-L290](file:///e:/Github/LearnTool/LearningAssistant.Core/Presenters/Pdf/PdfPresenter.cs#L220-L290) 与 [PdfZoomController.cs L75](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfZoomController.cs#L75)。
- `IPdfService.RenderPage` 的字节流调用点只有 [ExportService.cs L154、L383](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Services/Learning/ExportService.cs#L154)，方案保留该方法，导出链路不变。
- 撤销实现核对：[UndoStroke L1557-L1579](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfReaderNavigationManager.cs#L1557-L1579) 与 [UndoHighlightAsync L671-L734](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/PdfReaderHighlightManager.cs#L671-L734) **都不经过整页渲染**，只做「删除数据 + 重建图层 + Invalidate」，因此代次号/取消不会直接破坏撤销；唯一威胁是 R5 的异步加载晚到。
- 绘制写入点：[AnnotationToolHandler.cs L232-L349](file:///e:/Github/LearnTool/LearningAssistant.WinApp/Managers/Pdf/AnnotationToolHandler.cs#L232-L349) 直接写 `AnnotationBitmap` 并按 `activeBmp.Width/Height` 归一化，故 R1、R4 会直接影响绘制。

### 8.4 实施前置条件（硬门槛）

进入 Phase 2 前必须完成以下 5 项，否则不允许继续：

1. **标注层修订号**：异步 `LoadAnnotationsAsync` / `LoadSecondAnnotationsAsync` 结果应用前校验修订号（解决 R5）。
2. **绘制守卫**：`IsDrawing` / `IsDraggingSelection` 期间挂起新页面位图，`MouseUp` 后再应用（解决 R4、R8）。
3. **位图所有权契约**：接口注释 + 渲染器深拷贝 + 视图唯一释放点（解决 R1）。
4. **夜间模式原子迁移**：渲染、缓存、Paint 三处同一提交，反色只作用于页面位图（解决 R3、R12）。
5. **刷新路径整改**：清空标注不再依赖整页重渲染（解决 R7）。

### 8.5 绘制 / 撤销回归清单（每个阶段结束必测）

1. 铅笔、钢笔、马克笔、橡皮、矩形、椭圆、箭头、马赛克、文字标注各画 3 个，画面即时出现。
2. 切页再返回，所有标注位置与颜色一致。
3. 连续画 5 笔 → 撤销 5 次，逐笔消失；第 6 次提示「没有可撤销的操作」。
4. 交替执行「画笔 1 次 + 高亮 1 次」共 6 次，再撤销 6 次，顺序与实际操作一致。
5. 撤销后立即缩放、切页、再返回，被撤销内容不得复活（对应 R5、R8）。
6. 清空所有标注后立即翻页再返回，页面保持为空（对应 R7）。
7. 双页模式：右页绘制、撤销、选中、拖拽、夜间模式下颜色正确（对应 R6）。
8. 400% 缩放下绘制一笔，回到 100% 位置仍准确（对应 R11）。
9. 夜间模式画一笔 → 退出夜间，颜色/位置不变；导出标注图片背景为白色（对应 R3、R12）。
10. 快速连按下一页 20 次后回到某页，页码、画面、标注、撤销栈状态一致。

## 9. 后续项（本方案不含）

- 搜索：全文文本缓存 + 页内命中高亮 + 常驻搜索面板。
- 缩略图：改用已实现的 `GenerateVisibleThumbnailsAsync`（当前全量生成）。
- 高亮/标注：改为 Paint 矢量绘制，去掉整页 ARGB 位图。
- `PdfReaderForm.cs` 拆分（6011 行，`InitializeComponent` 约 1700 行）。
