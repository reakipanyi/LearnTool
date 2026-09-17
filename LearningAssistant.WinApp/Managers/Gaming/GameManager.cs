using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Feedback;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    /// <summary>
    /// 游戏管理器 - 负责猜词小游戏的逻辑
    /// </summary>
    public class GameManager
    {
        private readonly ILogger<GameManager>? _logger;
        private readonly ISoundService? _soundService;
        private readonly Action<int>? _onScoreChanged;
        private readonly Action<int>? _onXPChanged;
        private readonly Action? _onLevelUp;

        private int _score = 0;
        private Panel? _panelGame;
        private Label? _labelQuestion;
        private TextBox? _textBoxAnswer;
        private Label? _labelResult;
        private System.Windows.Forms.Timer? _gameTimer;
        private LearningItem? _currentItem;

        /// <summary>
        /// 事件：游戏提交答案
        /// </summary>
        public event Action<string, bool>? AnswerSubmitted;

        /// <summary>
        /// 构造函数
        /// </summary>
        public GameManager(
            ILogger<GameManager>? logger = null,
            ISoundService? soundService = null,
            Action<int>? onScoreChanged = null,
            Action<int>? onXPChanged = null,
            Action? onLevelUp = null)
        {
            _logger = logger;
            _soundService = soundService;
            _onScoreChanged = onScoreChanged;
            _onXPChanged = onXPChanged;
            _onLevelUp = onLevelUp;
        }

        /// <summary>
        /// 设置UI控件引用
        /// </summary>
        public void SetUI(Panel panelGame, Label labelQuestion, TextBox textBoxAnswer, Label labelResult, System.Windows.Forms.Timer timer)
        {
            _panelGame = panelGame;
            _labelQuestion = labelQuestion;
            _textBoxAnswer = textBoxAnswer;
            _labelResult = labelResult;
            _gameTimer = timer;
        }

        /// <summary>
        /// 设置当前学习项
        /// </summary>
        public void SetCurrentItem(LearningItem? item)
        {
            _currentItem = item;
        }

        /// <summary>
        /// 启动游戏
        /// </summary>
        public void Start()
        {
            _score = 0;
            _panelGame!.Visible = true;
            _gameTimer?.Start();
            NextQuestion();
        }

        /// <summary>
        /// 生成下一道题目
        /// </summary>
        public void NextQuestion()
        {
            if (_currentItem == null) return;

            _labelQuestion!.Text = $"❓ {_currentItem.GetMainContent()} 的意思是？";
            _textBoxAnswer!.Text = "";
            _labelResult!.Text = "";
        }

        /// <summary>
        /// 提交答案
        /// </summary>
        public void SubmitAnswer()
        {
            if (_currentItem == null) return;

            string userAnswer = _textBoxAnswer!.Text.Trim().ToLower();
            string correctAnswer = _currentItem.GetDisplayText().ToLower();

            bool isCorrect = correctAnswer.Contains(userAnswer) || userAnswer.Contains(correctAnswer);

            if (isCorrect)
            {
                _score += 10;
                _onScoreChanged?.Invoke(10);
                _onXPChanged?.Invoke(10);
                _labelResult!.Text = $"✅ 正确！得分: {_score}";
                _soundService?.PlaySuccess();
            }
            else
            {
                _labelResult!.Text = $"❌ 错误！正确答案: {_currentItem.GetDisplayText()}";
                _soundService?.PlayError();
            }

            AnswerSubmitted?.Invoke(userAnswer, isCorrect);
            NextQuestion();
        }

        /// <summary>
        /// 获取当前分数
        /// </summary>
        public int Score => _score;

        /// <summary>
        /// 获取是否为游戏状态
        /// </summary>
        public bool IsActive => _panelGame?.Visible ?? false;
    }
}
