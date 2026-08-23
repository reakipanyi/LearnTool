using LearningAssistant.Common.Events;
using LearningAssistant.Models.PanAnalysis;

namespace LearningAssistant.Services.PanAnalysis;

#region P2-4: 网盘整理事件总线事件

/// <summary>文件移动事件</summary>
public class PanFileMovedEvent : ApplicationEventBase
{
    public PanFileInfo File { get; init; } = new();
    public string OldPath { get; init; } = "";
    public string NewPath { get; init; } = "";
}

/// <summary>文件删除事件</summary>
public class PanFileDeletedEvent : ApplicationEventBase
{
    public PanFileInfo File { get; init; } = new();
}

/// <summary>文件重命名事件</summary>
public class PanFileRenamedEvent : ApplicationEventBase
{
    public string OldPath { get; init; } = "";
    public string NewPath { get; init; } = "";
    public bool IsFolder { get; init; }
}

/// <summary>文件夹创建事件</summary>
public class PanFolderCreatedEvent : ApplicationEventBase
{
    public string FolderPath { get; init; } = "";
    public string ParentPath { get; init; } = "/";
}

/// <summary>整理完成事件（执行完毕后发布，触发 AI 总结等）</summary>
public class PanOrganizeCompletedEvent : ApplicationEventBase
{
    public PanExecutionReport Report { get; init; } = new();
    public string Summary { get; init; } = "";
}

#endregion