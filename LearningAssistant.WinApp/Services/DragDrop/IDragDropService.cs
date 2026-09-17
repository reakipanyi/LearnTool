using LearningAssistant.Models.DragDrop;
using System.Windows.Forms;

namespace LearningAssistant.Services.DragDrop
{
    /// <summary>
    /// 拖拽管理服务接口
    /// 提供统一的拖拽处理功能，简化 WinForms 拖拽操作
    /// </summary>
    public interface IDragDropService
    {
        /// <summary>
        /// 注册为拖拽源
        /// </summary>
        /// <param name="control">源控件</param>
        /// <param name="dataProvider">数据提供函数</param>
        /// <param name="allowedEffects">允许的拖拽效果</param>
        void RegisterDragSource(Control control, Func<DragData?> dataProvider, DragDropEffect allowedEffects = DragDropEffect.All);

        /// <summary>
        /// 注册为拖拽目标
        /// </summary>
        /// <param name="control">目标控件</param>
        /// <param name="dropHandler">放下处理函数</param>
        /// <param name="dragEnterHandler">进入处理函数（可选）</param>
        /// <param name="allowedDataTypes">允许的数据类型</param>
        void RegisterDropTarget(
            Control control,
            Func<DragData, bool> dropHandler,
            Func<DragData, DragDropEffect>? dragEnterHandler = null,
            params DragDataType[] allowedDataTypes);

        /// <summary>
        /// 注销拖拽源
        /// </summary>
        void UnregisterDragSource(Control control);

        /// <summary>
        /// 注销拖拽目标
        /// </summary>
        void UnregisterDropTarget(Control control);

        /// <summary>
        /// 从 IDataObject 解析拖拽数据
        /// </summary>
        DragData ParseDragData(IDataObject dataObject);

        /// <summary>
        /// 创建拖拽数据对象
        /// </summary>
        IDataObject CreateDataObject(DragData dragData);

        /// <summary>
        /// 检查是否包含指定类型的数据
        /// </summary>
        bool HasData(IDataObject dataObject, DragDataType dataType);

        /// <summary>
        /// 获取支持的文件扩展名列表
        /// </summary>
        /// <param name="category">分类（image、document、audio等）</param>
        List<string> GetSupportedExtensions(string category);

        /// <summary>
        /// 拖拽开始事件
        /// </summary>
        event EventHandler<DragDropEventArgs>? DragStart;

        /// <summary>
        /// 拖拽完成事件
        /// </summary>
        event EventHandler<DragDropEventArgs>? DragComplete;

        /// <summary>
        /// 放下事件
        /// </summary>
        event EventHandler<DragDropEventArgs>? DragDropped;
    }
}
