using LearningAssistant.Models.AI;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// 对话上下文服务接口 - 管理多轮对话历史
    /// </summary>
    public interface IConversationContextService
    {
        /// <summary>
        /// 获取或创建当前会话
        /// </summary>
        MentorSession GetOrCreateSession(string userId, MentorPersonaType personaType = MentorPersonaType.Tutor);

        /// <summary>
        /// 获取当前会话
        /// </summary>
        MentorSession? CurrentSession { get; }

        /// <summary>
        /// 添加用户消息并获取AI回复
        /// </summary>
        Task<string> AddMessageAsync(string userMessage, CancellationToken cancellationToken = default);

        /// <summary>
        /// 切换导师角色
        /// </summary>
        void SwitchPersona(MentorPersonaType personaType);

        /// <summary>
        /// 设置学习上下文
        /// </summary>
        void SetLearningContext(string context);

        /// <summary>
        /// 清空当前会话
        /// </summary>
        void ClearCurrentSession();

        /// <summary>
        /// 清空指定用户的所有会话
        /// </summary>
        void ClearAllSessions(string userId);

        /// <summary>
        /// 获取历史消息数量
        /// </summary>
        int GetHistoryCount();

        /// <summary>
        /// 事件：收到新消息
        /// </summary>
        event EventHandler<ConversationTurn>? MessageReceived;
    }
}
