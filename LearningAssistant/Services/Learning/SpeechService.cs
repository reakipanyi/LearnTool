using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 语音服务
    /// 提供拼写检查和听写功能
    /// </summary>
    public class SpeechService : IDisposable
    {
        private readonly ILogger<SpeechService>? _logger;
        private SpeechRecognitionEngine? _recognizer;
        private SpeechSynthesizer? _synthesizer;
        private bool _disposed = false;
        private bool _isInitialized = false;

        public event EventHandler<DictationResultEventArgs>? DictationCompleted;
        public event EventHandler<string>? DictationError;

        public SpeechService(ILogger<SpeechService>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// 延迟初始化语音组件，仅在首次使用时加载
        /// </summary>
        private void EnsureInitialized()
        {
            if (_isInitialized || _disposed) return;
            
            try
            {
                InitializeSpeechComponents();
                _isInitialized = true;
                _logger?.LogInformation("Speech components initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize speech components");
                throw new InvalidOperationException("语音组件初始化失败，请检查系统语音功能是否正常", ex);
            }
        }

        #region 拼写检查

        public SpellingResult CheckSpelling(string word, string language = "en-US")
        {
            var result = new SpellingResult { OriginalWord = word };

            if (string.IsNullOrWhiteSpace(word))
            {
                result.IsCorrect = false;
                return result;
            }

            // 使用内置词典进行拼写检查
            var suggestions = GetSpellingSuggestions(word, language);
            result.IsCorrect = suggestions.Count == 0;
            result.Suggestions = suggestions;

            return result;
        }

        public List<string> GetSpellingSuggestions(string word, string language)
        {
            var suggestions = new List<string>();

            // 模拟拼写检查（实际应用中可以使用更强大的拼写检查库）
            var dictionary = GetCommonWords(language);
            
            // 完全匹配
            if (dictionary.Contains(word.ToLower()))
            {
                return suggestions;
            }

            // 查找相似词
            foreach (var dictWord in dictionary)
            {
                int distance = LevenshteinDistance(word.ToLower(), dictWord);
                if (distance <= 2)
                {
                    suggestions.Add(dictWord);
                }
            }

            // 按相似度排序
            suggestions.Sort((a, b) => 
                LevenshteinDistance(word.ToLower(), a).CompareTo(LevenshteinDistance(word.ToLower(), b)));

            return suggestions.Take(5).ToList();
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            int[,] dp = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
                }
            }

            return dp[s1.Length, s2.Length];
        }

        private List<string> GetCommonWords(string language)
        {
            if (language.StartsWith("zh"))
            {
                return new List<string> { "学习", "知识", "教育", "考试", "作业", "学校", "学生", "老师", "课程", "作业" };
            }

            // 英文常用词
            return new List<string>
            {
                "apple", "banana", "computer", "education", "friend", "happy", "important", 
                "journey", "knowledge", "language", "morning", "natural", "opportunity", 
                "practice", "question", "respect", "science", "teacher", "understand", 
                "vocabulary", "wonderful", "excellent", "beautiful", "important", 
                "knowledge", "learning", "progress", "success", "challenge", "improve"
            };
        }

        #endregion

        #region 听写功能

        public void StartDictation()
        {
            try
            {
                EnsureInitialized();
                
                if (_recognizer == null)
                {
                    _logger?.LogError("语音识别引擎未初始化");
                    OnDictationError("语音识别引擎未初始化");
                    return;
                }

                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                _logger?.LogInformation("听写已开始");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "启动听写失败");
                OnDictationError(ex.Message);
            }
        }

        public void StopDictation()
        {
            try
            {
                EnsureInitialized();
                
                if (_recognizer != null)
                {
                    _recognizer.RecognizeAsyncStop();
                    _logger?.LogInformation("听写已停止");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "停止听写失败");
            }
        }

        public async Task<DictationScore> StartDictationSession(string expectedText, int timeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<DictationScore>();
            string? recognizedText = null;

            EventHandler<SpeechRecognizedEventArgs> recognizedHandler = (sender, e) =>
            {
                recognizedText = e.Result.Text;
                _logger?.LogInformation("识别结果: {Text}", recognizedText);
                tcs.TrySetResult(ScoreDictation(expectedText, recognizedText ?? string.Empty));
            };

            EventHandler<SpeechRecognitionRejectedEventArgs> rejectedHandler = (sender, e) =>
            {
                _logger?.LogWarning("语音识别被拒绝");
                tcs.TrySetResult(ScoreDictation(expectedText, string.Empty));
            };

            try
            {
                EnsureInitialized();
                
                if (_recognizer == null)
                {
                    return new DictationScore(false, 0, string.Empty, "语音识别引擎未初始化");
                }

                _recognizer.SpeechRecognized += recognizedHandler;
                _recognizer.SpeechRecognitionRejected += rejectedHandler;

                using (cancellationToken.Register(() => 
                {
                    _recognizer?.RecognizeAsyncStop();
                    tcs.TrySetResult(new DictationScore(false, 0, string.Empty, "操作已取消"));
                }))
                {
                    _recognizer.RecognizeAsync(RecognizeMode.Single);

                    var delayTask = Task.Delay(timeoutSeconds * 1000, cancellationToken);
                    var completedTask = await Task.WhenAny(tcs.Task, delayTask);

                    _recognizer.RecognizeAsyncStop();

                    if (completedTask == delayTask)
                    {
                        return new DictationScore(false, 0, string.Empty, "超时");
                    }

                    return await tcs.Task;
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("听写会话已取消");
                return new DictationScore(false, 0, string.Empty, "操作已取消");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "听写会话失败");
                return new DictationScore(false, 0, string.Empty, ex.Message);
            }
            finally
            {
                if (_recognizer != null)
                {
                    _recognizer.SpeechRecognized -= recognizedHandler;
                    _recognizer.SpeechRecognitionRejected -= rejectedHandler;
                }
            }
        }

        public DictationScore ScoreDictation(string expectedText, string recognizedText)
        {
            if (string.IsNullOrWhiteSpace(expectedText))
            {
                return new DictationScore(true, 100, recognizedText, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(recognizedText))
            {
                return new DictationScore(false, 0, string.Empty, "未识别到语音");
            }

            // 计算相似度
            var expectedWords = expectedText.ToLower().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var recognizedWords = recognizedText.ToLower().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            int matchedWords = 0;
            foreach (var word in expectedWords)
            {
                if (recognizedWords.Contains(word))
                {
                    matchedWords++;
                }
            }

            int score = expectedWords.Length > 0 ? (matchedWords * 100) / expectedWords.Length : 100;
            bool passed = score >= 80;

            string message = passed 
                ? $"听写成功！得分: {score}分" 
                : $"听写未通过，得分: {score}分。请重试。";

            return new DictationScore(passed, score, recognizedText, message);
        }

        #endregion

        #region 语音合成

        public void Speak(string text, string voiceName = "", int rate = 0)
        {
            try
            {
                EnsureInitialized();
                
                if (_synthesizer == null)
                {
                    _logger?.LogError("语音合成引擎未初始化");
                    return;
                }

                if (!string.IsNullOrEmpty(voiceName))
                {
                    var voice = _synthesizer.GetInstalledVoices()
                        .FirstOrDefault(v => v.VoiceInfo.Name.Contains(voiceName, StringComparison.OrdinalIgnoreCase));
                    if (voice != null)
                    {
                        _synthesizer.SelectVoice(voice.VoiceInfo.Name);
                    }
                }

                _synthesizer.Rate = rate;
                _synthesizer.Speak(text);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "语音合成失败");
            }
        }

        public async Task SpeakAsync(string text, string voiceName = "", int rate = 0)
        {
            await Task.Run(() => Speak(text, voiceName, rate));
        }

        public List<string> GetAvailableVoices()
        {
            try
            {
                EnsureInitialized();
                
                if (_synthesizer == null)
                {
                    return new List<string>();
                }

                return _synthesizer.GetInstalledVoices()
                    .Select(v => v.VoiceInfo.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取可用语音列表失败");
                return new List<string>();
            }
        }

        #endregion

        #region 初始化和清理

        private void InitializeSpeechComponents()
        {
            // 初始化语音识别
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            _recognizer = new SpeechRecognitionEngine(culture);

            // 创建语法
            var dictationGrammar = new DictationGrammar();
            _recognizer.LoadGrammar(dictationGrammar);

            _recognizer.SpeechRecognized += (sender, e) =>
            {
                OnDictationCompleted(e.Result.Text, true);
            };

            _recognizer.SpeechRecognitionRejected += (sender, e) =>
            {
                OnDictationCompleted(string.Empty, false);
            };

            _recognizer.SetInputToDefaultAudioDevice();

            // 初始化语音合成
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    _recognizer?.Dispose();
                    _synthesizer?.Dispose();
                    _logger?.LogInformation("Speech components disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing speech components");
                }
            }

            _disposed = true;
        }

        #endregion

        #region 事件触发

        private void OnDictationCompleted(string text, bool success)
        {
            try
            {
                DictationCompleted?.Invoke(this, new DictationResultEventArgs(text, success));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error invoking DictationCompleted event");
            }
        }

        private void OnDictationError(string message)
        {
            try
            {
                DictationError?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error invoking DictationError event");
            }
        }

        #endregion
    }

    #region 数据模型

    public class SpellingResult
    {
        public string OriginalWord { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public List<string> Suggestions { get; set; } = new List<string>();
    }

    public class DictationScore
    {
        public bool Passed { get; }
        public int Score { get; }
        public string RecognizedText { get; }
        public string Message { get; }

        public DictationScore(bool passed, int score, string recognizedText, string message)
        {
            Passed = passed;
            Score = score;
            RecognizedText = recognizedText;
            Message = message;
        }
    }

    public class DictationResultEventArgs : EventArgs
    {
        public string Text { get; }
        public bool Success { get; }

        public DictationResultEventArgs(string text, bool success)
        {
            Text = text;
            Success = success;
        }
    }

    #endregion
}
