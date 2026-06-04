using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace UnifiedLearningAssistant.Services.Learning
{
    public interface IAdvancedSpeechService
    {
        void SpeakWithPhonetics(string text);
        void StartDictationWithFeedback(Action<string, double> onResult);
        void StopDictation();
        PronunciationResult CheckPronunciation(string word, string expected);
        List<PhoneticInfo> GetPhonetics(string word);
        void PlayPronunciation(string word);
        DictationScore StartDictationSessionWithScore(string expectedText, int timeoutSeconds = 30);
        void SpeakWithEmphasis(string text, Dictionary<string, int> emphasisWords);
        List<string> GetAvailableVoices();
        void SetVoice(string voiceName);
    }

    public class PhoneticInfo
    {
        public string Word { get; set; } = string.Empty;
        public string Phonetic { get; set; } = string.Empty;
        public string PartOfSpeech { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
    }

    public class PronunciationResult
    {
        public bool IsCorrect { get; set; }
        public double Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string ExpectedPhonetic { get; set; } = string.Empty;
        public string ActualPhonetic { get; set; } = string.Empty;
    }

    public class AdvancedSpeechService : IAdvancedSpeechService, IDisposable
    {
        private readonly ILogger<AdvancedSpeechService>? _logger;
        private SpeechRecognitionEngine? _recognizer;
        private SpeechSynthesizer? _synthesizer;
        private bool _disposed = false;
        private bool _isInitialized = false;

        public event EventHandler<DictationResultEventArgs>? DictationCompleted;
        public event EventHandler<string>? DictationError;

        public AdvancedSpeechService(ILogger<AdvancedSpeechService>? logger = null)
        {
            _logger = logger;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized || _disposed) return;
            
            try
            {
                _recognizer = new SpeechRecognitionEngine();
                var dictationGrammar = new DictationGrammar();
                _recognizer.LoadGrammar(dictationGrammar);
                _recognizer.SetInputToDefaultAudioDevice();

                _synthesizer = new SpeechSynthesizer();
                _synthesizer.SetOutputToDefaultAudioDevice();

                _recognizer.SpeechRecognized += (sender, e) =>
                {
                    OnDictationCompleted(e.Result.Text, true);
                };

                _recognizer.SpeechRecognitionRejected += (sender, e) =>
                {
                    OnDictationCompleted(string.Empty, false);
                };

                _isInitialized = true;
                _logger?.LogInformation("高级语音服务初始化成功");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "高级语音服务初始化失败");
            }
        }

        public void SpeakWithPhonetics(string text)
        {
            EnsureInitialized();
            
            if (_synthesizer == null)
            {
                _logger?.LogError("语音合成引擎未初始化");
                return;
            }

            try
            {
                var phoneticText = AddPhoneticGuide(text);
                _synthesizer.Speak(phoneticText);
                _logger?.LogInformation("带音标朗读完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "带音标朗读失败");
            }
        }

        public void StartDictationWithFeedback(Action<string, double> onResult)
        {
            EnsureInitialized();
            
            if (_recognizer == null)
            {
                _logger?.LogError("语音识别引擎未初始化");
                return;
            }

            try
            {
                _recognizer.SpeechRecognized += (sender, e) =>
                {
                    var confidence = e.Result.Confidence;
                    onResult?.Invoke(e.Result.Text, confidence);
                };

                _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                _logger?.LogInformation("带反馈的听写已开始");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "启动带反馈的听写失败");
            }
        }

        public void StopDictation()
        {
            try
            {
                _recognizer?.RecognizeAsyncStop();
                _logger?.LogInformation("听写已停止");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "停止听写失败");
            }
        }

        public PronunciationResult CheckPronunciation(string word, string expected)
        {
            var result = new PronunciationResult();

            try
            {
                var expectedClean = CleanText(expected.ToLower());
                var wordClean = CleanText(word.ToLower());

                var similarity = CalculateSimilarity(expectedClean, wordClean);
                result.Score = similarity;
                result.IsCorrect = similarity >= 0.8;

                if (result.IsCorrect)
                {
                    result.Feedback = "发音正确！";
                }
                else if (similarity >= 0.6)
                {
                    result.Feedback = "发音接近，请再试一次";
                }
                else
                {
                    result.Feedback = "发音需要改进，请参考标准发音";
                }

                _logger?.LogInformation("发音检查完成: {Word}, 相似度: {Similarity}", word, similarity);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "发音检查失败");
                result.Feedback = "检查失败，请重试";
            }

            return result;
        }

        public List<PhoneticInfo> GetPhonetics(string word)
        {
            var result = new List<PhoneticInfo>();

            try
            {
                var phoneticMap = new Dictionary<string, string>
                {
                    { "hello", "/həˈloʊ/" },
                    { "world", "/wɜːrld/" },
                    { "learning", "/ˈlɜːrnɪŋ/" },
                    { "computer", "/kəmˈpjuːtər/" },
                    { "programming", "/ˈproʊɡræmɪŋ/" },
                    { "knowledge", "/ˈnɑːlɪdʒ/" },
                    { "education", "/ˌedʒuˈkeɪʃn/" },
                    { "technology", "/tekˈnɑːlədʒi/" },
                    { "information", "/ˌɪnfərˈmeɪʃn/" },
                    { "development", "/dɪˈveləpmənt/" }
                };

                if (phoneticMap.TryGetValue(word.ToLower(), out var phonetic))
                {
                    result.Add(new PhoneticInfo
                    {
                        Word = word,
                        Phonetic = phonetic,
                        PartOfSpeech = "noun",
                        AudioUrl = string.Empty
                    });
                }

                _logger?.LogDebug("获取音标: {Word}", word);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取音标失败");
            }

            return result;
        }

        public void PlayPronunciation(string word)
        {
            EnsureInitialized();
            
            if (_synthesizer == null)
            {
                _logger?.LogError("语音合成引擎未初始化");
                return;
            }

            try
            {
                _synthesizer.Speak(word);
                _logger?.LogInformation("播放发音: {Word}", word);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "播放发音失败");
            }
        }

        public DictationScore StartDictationSessionWithScore(string expectedText, int timeoutSeconds = 30)
        {
            EnsureInitialized();
            
            if (_recognizer == null)
            {
                return new DictationScore(false, 0, string.Empty, "语音识别引擎未初始化");
            }

            string? recognizedText = null;

            EventHandler<SpeechRecognizedEventArgs> recognizedHandler = (sender, e) =>
            {
                recognizedText = e.Result.Text;
                _logger?.LogInformation("识别结果: {Text}", recognizedText);
            };

            try
            {
                _recognizer.SpeechRecognized += recognizedHandler;

                _recognizer.RecognizeAsync(RecognizeMode.Single);

                System.Threading.Thread.Sleep(timeoutSeconds * 1000);

                _recognizer.RecognizeAsyncStop();

                if (string.IsNullOrEmpty(recognizedText))
                {
                    return new DictationScore(false, 0, string.Empty, "超时或未识别");
                }

                return ScoreDictation(expectedText, recognizedText);
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
                }
            }
        }

        public void SpeakWithEmphasis(string text, Dictionary<string, int> emphasisWords)
        {
            EnsureInitialized();
            
            if (_synthesizer == null)
            {
                _logger?.LogError("语音合成引擎未初始化");
                return;
            }

            try
            {
                foreach (var kvp in emphasisWords)
                {
                    _synthesizer.Rate = kvp.Value > 0 ? -2 : 0;
                    _synthesizer.Speak(kvp.Key);
                    _synthesizer.Rate = 0;
                }

                _synthesizer.Speak(text);
                _logger?.LogInformation("带强调朗读完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "带强调朗读失败");
            }
        }

        public List<string> GetAvailableVoices()
        {
            EnsureInitialized();
            
            try
            {
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

        public void SetVoice(string voiceName)
        {
            EnsureInitialized();
            
            if (_synthesizer == null)
            {
                _logger?.LogError("语音合成引擎未初始化");
                return;
            }

            try
            {
                var voice = _synthesizer.GetInstalledVoices()
                    .FirstOrDefault(v => v.VoiceInfo.Name.Contains(voiceName, StringComparison.OrdinalIgnoreCase));
                
                if (voice != null)
                {
                    _synthesizer.SelectVoice(voice.VoiceInfo.Name);
                    _logger?.LogInformation("语音已设置为: {VoiceName}", voice.VoiceInfo.Name);
                }
                else
                {
                    _logger?.LogWarning("未找到语音: {VoiceName}", voiceName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "设置语音失败");
            }
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
                    _logger?.LogInformation("AdvancedSpeechService disposed");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disposing AdvancedSpeechService");
                }
            }

            _disposed = true;
        }

        private string AddPhoneticGuide(string text)
        {
            var words = text.Split(' ');
            var result = new List<string>();

            foreach (var word in words)
            {
                var phonetics = GetPhonetics(word);
                if (phonetics.Any())
                {
                    result.Add($"{word} ({phonetics[0].Phonetic})");
                }
                else
                {
                    result.Add(word);
                }
            }

            return string.Join(" ", result);
        }

        private string CleanText(string text)
        {
            return Regex.Replace(text, @"[^a-zA-Z\s]", "").Trim().ToLower();
        }

        private double CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0;

            var longer = s1.Length > s2.Length ? s1 : s2;
            var shorter = s1.Length > s2.Length ? s2 : s1;

            var longerLength = longer.Length;

            if (longerLength == 0)
                return 1.0;

            return (longerLength - LevenshteinDistance(longer, shorter)) / (double)longerLength;
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            var costs = new int[s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
            {
                int lastValue = i;
                for (int j = 0; j <= s2.Length; j++)
                {
                    if (i == 0)
                        costs[j] = j;
                    else if (j > 0)
                    {
                        int newValue = costs[j - 1];
                        if (s1[i - 1] != s2[j - 1])
                            newValue = Math.Min(Math.Min(newValue, lastValue), costs[j]) + 1;
                        costs[j - 1] = lastValue;
                        lastValue = newValue;
                    }
                }
                if (i > 0)
                    costs[s2.Length] = lastValue;
            }

            return costs[s2.Length];
        }

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

        private DictationScore ScoreDictation(string expected, string actual)
        {
            var expectedClean = CleanText(expected);
            var actualClean = CleanText(actual);

            var similarity = CalculateSimilarity(expectedClean, actualClean);
            var isCorrect = similarity >= 0.8;

            return new DictationScore(isCorrect, (int)(similarity * 100), actual, string.Empty);
        }
    }
}