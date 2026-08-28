# PdfReaderNavigationManager 拆分计划

## 现状

`PdfReaderNavigationManager` 3271 行，承担以下职责：
- 缩放控制（Zoom / ZoomByMouseWheel / ResetZoom）
- 页面导航（NextPage / PreviousPage / NavigateToPage）
- 页面切换动画（PageTransition）
- 10+ 种标注工具（Highlight / Pen / Rectangle / Ellipse / Arrow / Mosaic / Strikethrough / Text / Eraser / LaserPointer / Spotlight / ImageEmbed / Checklist）
- 标注的 MouseDown / MouseMove / MouseUp 事件处理
- 标注选中、拖拽、缩放、删除
- 高亮交互（选中、拖拽、缩放）
- 文字注解交互（选中、拖拽、缩放、编辑）
- 橡皮擦模式（悬停检测、区域擦除）
- 激光笔、聚光灯模式
- 撤销栈管理
- 导航面板拖拽

## 拆分方案

### 1. PdfZoomController（~150行）
- 提取字段：`_zoomLevel`, `_imageOffset`, `_isLocked`
- 提取方法：`Zoom()`, `ZoomByMouseWheel()`, `ResetZoom()`, `RenderPageAtZoomAsync()`, `ToggleLockView()`
- 依赖：`IPdfReaderFormAccess`, `ILogger`

### 2. AnnotationToolHandler（~800行）
- 为每种工具实现策略接口 `IAnnotationToolHandler`
- 接口：`void MouseDown(Point)`, `void MouseMove(Point)`, `void MouseUp(Point)`, `void Paint(Graphics, Rectangle, int)`
- 实现类：`HighlightToolHandler`, `PenToolHandler`, `ShapeToolHandler`, `EraserToolHandler`, `TextToolHandler`, `LaserPointerToolHandler`, `SpotlightToolHandler`
- 工厂类：`AnnotationToolHandlerFactory` 根据 `AnnotationToolMode` 创建对应 handler

### 3. AnnotationSelectionManager（~600行）
- 提取字段：`_selectedStroke`, `_selectedStrokeIndex`, `_selectedHighlight`, `_selectedHighlightIndex`, `_selectedText`, `_selectedTextIndex`, `_selectionState`, `_selectionDragStart` 等
- 提取方法：`HandleSelectModeClick()`, `HandleSelectionDragMove()`, `HandleHighlightDragMove()`, `HandleTextDragMove()`, `HandleSelectionResize()`, `HandleHighlightResize()`, `HandleTextResize()`, `HitTestStroke()`, `HitTestHighlight()`, `HitTestText()`, `ClearSelection()`, `DeleteSelectedStroke()`, `DeleteSelectedText()`
- 依赖：`IPdfReaderFormAccess`, `ILogger`, `IHighlightService`

### 4. PageTransitionAnimator（~60行）
- 提取字段：`_isAnimating`, `_transitionStep`, `_transitionFadeOut`
- 提取方法：`StartPageTransition()`, `PageTransitionTimer_Tick()`
- 依赖：`IPdfReaderFormAccess`, `ILogger`

### 5. AnnotationLayerManager（~200行）
- 提取字段：`_annotationBitmap`, `_annotationGraphics`, `_secondAnnotationBitmap`, `_secondAnnotationGraphics`
- 提取方法：`EnsureAnnotationBitmap()`, `CleanupAnnotationBitmap()`, `CleanupSecondAnnotationBitmap()`, `LoadAnnotationsForCurrentPage()`, `ApplyLoadedAnnotationBitmap()`, `ApplySecondLoadedAnnotationBitmap()`, `ClearAllStrokes()`, `DrawAnnotations()`, `DrawAnnotationsToGraphics()`, `DrawSpotlightOverlay()`
- 依赖：`IPdfReaderFormAccess`, `ILogger`

### 6. UndoStackManager<T>（通用泛型类，~50行）
- 提取：`PushStrokeToUndoStack()`, `CanUndoStroke()`, `UndoStroke()` 的撤销栈逻辑
- 使用 `LinkedList<T>` 实现固定大小栈

### 7. 导航面板拖拽保留在 NavigationManager 或移到独立类

## 迁移步骤

1. 创建上述新类，PdfReaderNavigationManager 持有这些类的实例（组合优于继承）
2. 逐个将字段和方法迁移到新类
3. 暴露新类的方法供 PdfReaderNavigationManager 委托调用
4. 逐步验证每个功能模块
5. 最终 PdfReaderNavigationManager 缩减至 ~500 行（协调调度）