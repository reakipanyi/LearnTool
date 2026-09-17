using System.Threading;
using System.Threading.Tasks;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// AI 角色类型 - 定义统一 AI 对话入口支持的角色
    /// </summary>
    public enum AIRoleType
    {
        /// <summary>
        /// 导师 - 解答问题、解释概念
        /// </summary>
        Mentor,

        /// <summary>
        /// 费曼教练 - 检验用户解释是否清晰易懂
        /// </summary>
        Feynman,

        /// <summary>
        /// 苏格拉底 - 通过提问引导思考
        /// </summary>
        Socratic,

        /// <summary>
        /// 提示提供者 - 给出渐进式学习提示
        /// </summary>
        HintProvider
    }

    /// <summary>
    /// AI 对话请求 - 统一对话入口的输入参数
    /// </summary>
    public class AIConversationRequest
    {
        /// <summary>
        /// 对话角色
        /// </summary>
        public AIRoleType Role { get; set; } = AIRoleType.Mentor;

        /// <summary>
        /// 用户消息内容
        /// </summary>
        public string UserMessage { get; set; } = string.Empty;

        /// <summary>
        /// 学习上下文（可选）- 提供当前学习内容、进度等背景信息
        /// </summary>
        public string? Context { get; set; }
    }

    /// <summary>
    /// AI 对话响应 - 统一对话入口的返回结果
    /// </summary>
    public class AIConversationResponse
    {
        /// <summary>
        /// AI 回复内容
        /// </summary>
        public string Reply { get; set; } = string.Empty;

        /// <summary>
        /// 建议的后续操作列表
        /// </summary>
        public List<string> SuggestedActions { get; set; } = new();

        /// <summary>
        /// 产生回复的角色
        /// </summary>
        public AIRoleType Role { get; set; } = AIRoleType.Mentor;
    }

    /// <summary>
    /// AI 整合服务接口 - 统一 AI 对话入口，聚合分散的 AI 能力
    /// （对话上下文、费曼历史、渐进式提示等）
    /// </summary>
    public interface IAIHubService
    {
        /// <summary>
        /// 当前对话角色
        /// </summary>
        AIRoleType CurrentRole { get; }

        /// <summary>
        /// 统一对话入口 - 根据请求中的角色路由到对应 AI 能力并返回响应
        /// </summary>
        /// <param name="request">对话请求</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>对话响应</returns>
        Task<AIConversationResponse> SendAsync(AIConversationRequest request, CancellationToken ct = default);

        /// <summary>
        /// 获取当前可用的角色列表
        /// </summary>
        List<AIRoleType> GetAvailableRoles();

        /// <summary>
        /// 切换当前对话角色
        /// </summary>
        /// <param name="role">目标角色</param>
        void SwitchRole(AIRoleType role);

        /// <summary>
        /// 事件：收到 AI 响应
        /// </summary>
        event EventHandler<AIConversationResponse>? ResponseReceived;
    }
}
