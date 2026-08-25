namespace LearningAssistant.Managers
{
    /// <summary>
    /// 列表项选中事件参数
    /// </summary>
    public class ItemSelectedEventArgs : EventArgs
    {
        public int Index { get; }

        public ItemSelectedEventArgs(int index)
        {
            Index = index;
        }
    }
}
