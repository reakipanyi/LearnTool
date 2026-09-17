using LearningAssistant.Models.DragDrop;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Windows.Forms;

namespace LearningAssistant.Services.DragDrop
{
    /// <summary>
    /// 拖拽管理服务实现
    /// </summary>
    public class DragDropService : IDragDropService
    {
        private readonly ILogger<DragDropService>? _logger;
        private readonly Dictionary<Control, DragSourceInfo> _dragSources = new();
        private readonly Dictionary<Control, DropTargetInfo> _dropTargets = new();

        public event EventHandler<DragDropEventArgs>? DragStart;
        public event EventHandler<DragDropEventArgs>? DragComplete;
        public event EventHandler<DragDropEventArgs>? DragDropped;

        public DragDropService(ILogger<DragDropService>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void RegisterDragSource(Control control, Func<DragData?> dataProvider, DragDropEffect allowedEffects = DragDropEffect.All)
        {
            if (control == null || dataProvider == null)
                return;

            UnregisterDragSource(control);

            var info = new DragSourceInfo
            {
                Control = control,
                DataProvider = dataProvider,
                AllowedEffects = allowedEffects
            };

            control.MouseDown += OnDragSourceMouseDown;
            _dragSources[control] = info;

            _logger?.LogDebug("已注册拖拽源: {Control}", control.Name ?? control.GetType().Name);
        }

        /// <inheritdoc/>
        public void RegisterDropTarget(
            Control control,
            Func<DragData, bool> dropHandler,
            Func<DragData, DragDropEffect>? dragEnterHandler = null,
            params DragDataType[] allowedDataTypes)
        {
            if (control == null || dropHandler == null)
                return;

            UnregisterDropTarget(control);

            var info = new DropTargetInfo
            {
                Control = control,
                DropHandler = dropHandler,
                DragEnterHandler = dragEnterHandler,
                AllowedDataTypes = allowedDataTypes.ToList()
            };

            control.AllowDrop = true;
            control.DragEnter += OnDropTargetDragEnter;
            control.DragOver += OnDropTargetDragOver;
            control.DragDrop += OnDropTargetDragDrop;
            control.DragLeave += OnDropTargetDragLeave;
            _dropTargets[control] = info;

            _logger?.LogDebug("已注册拖拽目标: {Control}", control.Name ?? control.GetType().Name);
        }

        /// <inheritdoc/>
        public void UnregisterDragSource(Control control)
        {
            if (control == null)
                return;

            if (_dragSources.ContainsKey(control))
            {
                control.MouseDown -= OnDragSourceMouseDown;
                _dragSources.Remove(control);
                _logger?.LogDebug("已注销拖拽源: {Control}", control.Name ?? control.GetType().Name);
            }
        }

        /// <inheritdoc/>
        public void UnregisterDropTarget(Control control)
        {
            if (control == null)
                return;

            if (_dropTargets.ContainsKey(control))
            {
                control.DragEnter -= OnDropTargetDragEnter;
                control.DragOver -= OnDropTargetDragOver;
                control.DragDrop -= OnDropTargetDragDrop;
                control.DragLeave -= OnDropTargetDragLeave;
                _dropTargets.Remove(control);
                _logger?.LogDebug("已注销拖拽目标: {Control}", control.Name ?? control.GetType().Name);
            }
        }

        /// <inheritdoc/>
        public DragData ParseDragData(IDataObject dataObject)
        {
            var result = new DragData();

            try
            {
                if (dataObject.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[]?)dataObject.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        result.DataType = DragDataType.File;
                        result.FilePaths.AddRange(files);
                        result.FormatName = DataFormats.FileDrop;
                        return result;
                    }
                }

                if (dataObject.GetDataPresent(DataFormats.Text))
                {
                    var text = (string?)dataObject.GetData(DataFormats.Text);
                    if (!string.IsNullOrEmpty(text))
                    {
                        result.DataType = DragDataType.Text;
                        result.TextContent = text;
                        result.FormatName = DataFormats.Text;
                        return result;
                    }
                }

                if (dataObject.GetDataPresent(DataFormats.Html))
                {
                    var html = (string?)dataObject.GetData(DataFormats.Html);
                    if (!string.IsNullOrEmpty(html))
                    {
                        result.DataType = DragDataType.Html;
                        result.TextContent = html;
                        result.FormatName = DataFormats.Html;
                        return result;
                    }
                }

                if (dataObject.GetDataPresent(typeof(DragData).FullName))
                {
                    var customData = dataObject.GetData(typeof(DragData).FullName);
                    if (customData is DragData dragData)
                    {
                        return dragData;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "解析拖拽数据失败");
            }

            return result;
        }

        /// <inheritdoc/>
        public IDataObject CreateDataObject(DragData dragData)
        {
            var dataObject = new DataObject();

            try
            {
                switch (dragData.DataType)
                {
                    case DragDataType.File:
                        if (dragData.FilePaths.Count > 0)
                        {
                            dataObject.SetData(DataFormats.FileDrop, dragData.FilePaths.ToArray());
                        }
                        break;

                    case DragDataType.Text:
                        dataObject.SetData(DataFormats.Text, dragData.TextContent);
                        break;

                    case DragDataType.Html:
                        dataObject.SetData(DataFormats.Html, dragData.TextContent);
                        break;

                    default:
                        dataObject.SetData(typeof(DragData).FullName, dragData);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建拖拽数据对象失败");
            }

            return dataObject;
        }

        /// <inheritdoc/>
        public bool HasData(IDataObject dataObject, DragDataType dataType)
        {
            try
            {
                return dataType switch
                {
                    DragDataType.File => dataObject.GetDataPresent(DataFormats.FileDrop),
                    DragDataType.Text => dataObject.GetDataPresent(DataFormats.Text),
                    DragDataType.Html => dataObject.GetDataPresent(DataFormats.Html),
                    DragDataType.Image => dataObject.GetDataPresent(DataFormats.Bitmap),
                    _ => dataObject.GetDataPresent(typeof(DragData).FullName)
                };
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public List<string> GetSupportedExtensions(string category)
        {
            return category.ToLower() switch
            {
                "image" => new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".ico" },
                "document" => new List<string> { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".md", ".xls", ".xlsx", ".ppt", ".pptx" },
                "audio" => new List<string> { ".mp3", ".wav", ".wma", ".aac", ".flac", ".ogg", ".m4a" },
                "video" => new List<string> { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" },
                "archive" => new List<string> { ".zip", ".rar", ".7z", ".tar", ".gz" },
                "ebook" => new List<string> { ".pdf", ".epub", ".mobi", ".azw", ".azw3", ".txt" },
                "spreadsheet" => new List<string> { ".xls", ".xlsx", ".csv", ".ods" },
                _ => new List<string>()
            };
        }

        #region 私有方法 - 事件处理

        private void OnDragSourceMouseDown(object? sender, MouseEventArgs e)
        {
            if (sender is not Control control || !_dragSources.TryGetValue(control, out var info))
                return;

            if (e.Button != MouseButtons.Left)
                return;

            try
            {
                var dragData = info.DataProvider?.Invoke();
                if (dragData == null)
                    return;

                dragData.SourceId = control.Name ?? control.GetType().Name;
                dragData.AllowedEffect = info.AllowedEffects;

                var args = new DragDropEventArgs(dragData);
                DragStart?.Invoke(this, args);

                if (args.Handled)
                    return;

                var dataObject = CreateDataObject(dragData);
                var effects = ConvertDragDropEffect(info.AllowedEffects);
                control.DoDragDrop(dataObject, effects);

                DragComplete?.Invoke(this, new DragDropEventArgs(dragData));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "拖拽开始失败");
            }
        }

        private void OnDropTargetDragEnter(object? sender, DragEventArgs e)
        {
            if (sender is not Control control || !_dropTargets.TryGetValue(control, out var info))
                return;

            try
            {
                var dragData = ParseDragData(e.Data);
                dragData.TargetId = control.Name ?? control.GetType().Name;

                if (info.AllowedDataTypes.Count > 0 && !info.AllowedDataTypes.Contains(dragData.DataType))
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }

                if (info.DragEnterHandler != null)
                {
                    var effect = info.DragEnterHandler(dragData);
                    e.Effect = ConvertDragDropEffect(effect);
                }
                else
                {
                    e.Effect = ConvertDragDropEffect(DragDropEffect.Copy);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "拖拽进入处理失败");
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDropTargetDragOver(object? sender, DragEventArgs e)
        {
            if (sender is not Control control || !_dropTargets.TryGetValue(control, out var info))
                return;

            try
            {
                var dragData = ParseDragData(e.Data);

                if (info.AllowedDataTypes.Count > 0 && !info.AllowedDataTypes.Contains(dragData.DataType))
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }

                if (info.DragEnterHandler != null)
                {
                    var effect = info.DragEnterHandler(dragData);
                    e.Effect = ConvertDragDropEffect(effect);
                }
                else
                {
                    e.Effect = DragDropEffects.Copy;
                }
            }
            catch
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDropTargetDragDrop(object? sender, DragEventArgs e)
        {
            if (sender is not Control control || !_dropTargets.TryGetValue(control, out var info))
                return;

            try
            {
                var dragData = ParseDragData(e.Data);
                dragData.TargetId = control.Name ?? control.GetType().Name;

                var success = info.DropHandler?.Invoke(dragData) ?? false;

                var args = new DragDropEventArgs(dragData)
                {
                    Handled = success
                };
                DragDropped?.Invoke(this, args);

                e.Effect = success ? DragDropEffects.Copy : DragDropEffects.None;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "放下处理失败");
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDropTargetDragLeave(object? sender, EventArgs e)
        {
        }

        private static DragDropEffects ConvertDragDropEffect(DragDropEffect effect)
        {
            var result = DragDropEffects.None;
            if (effect.HasFlag(DragDropEffect.Copy))
                result |= DragDropEffects.Copy;
            if (effect.HasFlag(DragDropEffect.Move))
                result |= DragDropEffects.Move;
            if (effect.HasFlag(DragDropEffect.Link))
                result |= DragDropEffects.Link;
            return result;
        }

        private static DragDropEffect ConvertDragDropEffect(DragDropEffects effect)
        {
            var result = DragDropEffect.None;
            if (effect.HasFlag(DragDropEffects.Copy))
                result |= DragDropEffect.Copy;
            if (effect.HasFlag(DragDropEffects.Move))
                result |= DragDropEffect.Move;
            if (effect.HasFlag(DragDropEffects.Link))
                result |= DragDropEffect.Link;
            return result;
        }

        private class DragSourceInfo
        {
            public Control Control { get; set; } = null!;
            public Func<DragData?> DataProvider { get; set; } = null!;
            public DragDropEffect AllowedEffects { get; set; }
        }

        private class DropTargetInfo
        {
            public Control Control { get; set; } = null!;
            public Func<DragData, bool> DropHandler { get; set; } = null!;
            public Func<DragData, DragDropEffect>? DragEnterHandler { get; set; }
            public List<DragDataType> AllowedDataTypes { get; set; } = new();
        }

        #endregion
    }
}
