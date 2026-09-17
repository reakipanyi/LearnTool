using LearningAssistant.Models.AI;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// 对话上下文服务实现 - 管理多轮对话历史
    /// 使用现有 IAiQuestionService 实现AI对话
    /// </summary>
    public class ConversationContextService : IConversationContextService
    {
        private readonly IAiQuestionService _aiService;
        private readonly IUserSessionService _userSessionService;
        private readonly ILogger<ConversationContextService>? _logger;

        private MentorSession? _currentSession;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private const int MaxHistoryTurns = 20;
        private const int ContextTruncateTurns = 10;

        public event EventHandler<ConversationTurn>? MessageReceived;

        public ConversationContextService(
            IAiQuestionService aiService,
            IUserSessionService userSessionService,
            ILogger<ConversationContextService>? logger = null)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _userSessionService = userSessionService ?? throw new ArgumentNullException(nameof(userSessionService));
            _logger = logger;
        }

        /// <summary>
        /// 获取当前会话
        /// </summary>
        public MentorSession? CurrentSession => _currentSession;

        /// <summary>
        /// 获取或创建当前会话
        /// </summary>
        public MentorSession GetOrCreateSession(string userId, MentorPersonaType personaType = MentorPersonaType.Tutor)
        {
            _semaphore.Wait();
            try
            {
                if (_currentSession == null || _currentSession.UserId != userId)
                {
                    _currentSession = new MentorSession
                    {
                        UserId = userId,
                        Persona = new MentorPersona { Type = personaType }
                    };
                    _logger?.LogInformation("创建新导师会话: 用户 {UserId}, 角色 {Persona}", userId, personaType);
                }
                return _currentSession;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 添加用户消息并获取AI回复
        /// </summary>
        public async Task<string> AddMessageAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return string.Empty;

            var userId = _userSessionService.CurrentUserId;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var session = GetOrCreateSession(userId);

                var turn = new ConversationTurn
                {
                    UserMessage = userMessage.Trim(),
                    Type = GetConversationType(session.Persona.Type)
                };

                try
                {
                    _logger?.LogDebug("发送AI请求: 用户 {UserId}, 消息长度 {Length}", userId, userMessage.Length);

                    // 构建上下文 - 使用角色提示词 + 对话历史
                    var systemPrompt = session.Persona.GetSystemPrompt();
                    var historyContext = session.BuildContextString(ContextTruncateTurns);
                    var fullContext = string.IsNullOrEmpty(historyContext)
                        ? systemPrompt
                        : $"{systemPrompt}\n\n{historyContext}";

                    // 如果有学习上下文，添加进去
                    if (!string.IsNullOrEmpty(session.LearningContext))
                    {
                        fullContext = $"当前学习内容：{session.LearningContext}\n\n{fullContext}";
                    }

                    // 调用现有AI服务
                    var response = await _aiService.AskAsync(userMessage, fullContext, cancellationToken);

                    turn.AiResponse = response;
                    turn.Confidence = string.IsNullOrEmpty(response) ? 0 : 1.0;

                    // 添加到历史
                    session.AddTurn(turn);

                    // 限制历史长度
                    TrimHistory(session);

                    _logger?.LogDebug("AI回复: 长度 {Length}, 历史轮次 {Count}", response.Length, session.TurnCount);

                    // 触发事件
                    MessageReceived?.Invoke(this, turn);

                    return response;
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogWarning("AI请求取消");
                    // 取消时不记录turn到历史，避免污染后续对话上下文
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "AI请求失败");
                    turn.AiResponse = "抱歉，服务暂时不可用，请稍后再试。";
                    // 失败时也触发事件让界面停止加载并展示反馈，
                    // 但不写入历史，避免失败响应污染后续对话上下文
                    MessageReceived?.Invoke(this, turn);
                    return turn.AiResponse;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 切换导师角色
        /// </summary>
        public void SwitchPersona(MentorPersonaType personaType)
        {
            var userId = _userSessionService.CurrentUserId;
            var session = GetOrCreateSession(userId);

            if (session.Persona.Type != personaType)
            {
                session.Persona = new MentorPersona { Type = personaType };
                _logger?.LogInformation("切换导师角色: {Persona}", personaType);
            }
        }

        /// <summary>
        /// 设置学习上下文
        /// </summary>
        public void SetLearningContext(string context)
        {
            var userId = _userSessionService.CurrentUserId;
            var session = GetOrCreateSession(userId);

            session.LearningContext = context;
            _logger?.LogDebug("设置学习上下文: {Context}", context.Length > 50 ? context.Substring(0, 50) + "..." : context);
        }

        /// <summary>
        /// 清空当前会话
        /// </summary>
        public void ClearCurrentSession()
        {
            if (_currentSession != null)
            {
                _logger?.LogInformation("清空导师会话: {SessionId}", _currentSession.SessionId);
                _currentSession.Clear();
            }
        }

        /// <summary>
        /// 清空指定用户的所有会话
        /// </summary>
        public void ClearAllSessions(string userId)
        {
            _semaphore.Wait();
            try
            {
                if (_currentSession?.UserId == userId)
                {
                    _currentSession.Clear();
                }
            }
            finally
            {
                _semaphore.Release();
            }
            _logger?.LogInformation("清空用户所有会话: {UserId}", userId);
        }

        /// <summary>
        /// 获取历史消息数量
        /// </summary>
        public int GetHistoryCount()
        {
            return _currentSession?.TurnCount ?? 0;
        }

        /// <summary>
        /// 根据角色获取对话类型
        /// </summary>
        private static ConversationType GetConversationType(MentorPersonaType personaType)
        {
            return personaType switch
            {
                MentorPersonaType.Socratic => ConversationType.SocraticGuiding,
                MentorPersonaType.Feynman => ConversationType.FeynmanCheck,
                MentorPersonaType.Diagnostician => ConversationType.Diagnosis,
                _ => ConversationType.QA
            };
        }

        /// <summary>
        /// 限制历史长度
        /// </summary>
        private static void TrimHistory(MentorSession session)
        {
            while (session.History.Count > MaxHistoryTurns)
            {
                session.History.RemoveAt(0);
            }
        }
    }
}
