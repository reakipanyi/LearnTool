using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading;

namespace LearningAssistant.Services.TTS
{
    public class SpeechCoordinator : ISpeechCoordinator
    {
        private readonly ILogger<SpeechCoordinator>? _logger;
        private readonly ITTSService? _ttsService;
        private readonly TtsConfig _ttsConfig;
        
        private readonly ConcurrentQueue<SpeechQueueItem> _pronunciationQueue = new();
        private volatile int _isProcessingQueue = 0;
        private volatile string _currentSpeakKey = string.Empty;
        private CancellationTokenSource? _stopCts = new();
        
        public event EventHandler<SpeakStateChangedEventArgs>? SpeakStateChanged;

        public bool IsSpeaking => _ttsService?.IsSpeaking ?? false;

        public string CurrentSpeakKey => _currentSpeakKey;

        public SpeechCoordinator(ILogger<SpeechCoordinator>? logger, ITTSService? ttsService, TtsConfig ttsConfig)
        {
            _logger = logger;
            _ttsService = ttsService;
            _ttsConfig = ttsConfig ?? new TtsConfig();
        }

        public Task SpeakAsync(string text, string language, CancellationToken cancellationToken = default, string? speakKey = null)
        {
            return SpeakAsync(text, language, null, cancellationToken, speakKey);
        }

        public Task SpeakAsync(string text, string language, string? explanation, CancellationToken cancellationToken = default, string? speakKey = null)
        {
            _logger?.LogInformation("=== TTS INPUT LOG ===");
            _logger?.LogInformation("TTS SpeakAsync called: text='{Text}', language='{Language}', explanation='{Explanation}', speakKey='{SpeakKey}', textLength={TextLength}",
                text, language, explanation, speakKey ?? "__GLOBAL__", text?.Length ?? 0);
            _logger?.LogInformation("TTS available: {Available}", _ttsService?.Available ?? false);

            if (_ttsService == null || !_ttsService.Available)
            {
                _logger?.LogWarning("TTS service is not available, cannot speak");
                return Task.CompletedTask;
            }

            var queueItem = new SpeechQueueItem(text, language, explanation, speakKey ?? "__GLOBAL__");
            _pronunciationQueue.Enqueue(queueItem);
            
            _logger?.LogInformation("TTS item enqueued, queue count={QueueCount}", _pronunciationQueue.Count);
            
            _ = ProcessQueueAsync(cancellationToken);
            
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _pronunciationQueue.Clear();
            
            _stopCts?.Cancel();
            
            await (_ttsService?.StopAsync() ?? Task.CompletedTask);
            
            while (_isProcessingQueue != 0)
            {
                await Task.Delay(10);
            }
            
            _stopCts?.Dispose();
            _stopCts = new CancellationTokenSource();
            
            UpdateSpeakState(string.Empty, false);
        }

        public Task PreloadAsync(string text, string language)
        {
            if (_ttsService == null || !_ttsService.Available)
                return Task.CompletedTask;

            return _ttsService.SpeakToCacheAsync(text, language, speed: _ttsConfig.Speed);
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _isProcessingQueue, 1, 0) != 0)
                return;

            try
            {
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopCts?.Token ?? CancellationToken.None);
                
                while (_pronunciationQueue.TryDequeue(out var item) && !linkedCts.Token.IsCancellationRequested)
                {
                    UpdateSpeakState(item.SpeakKey, true);

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(item.Text))
                        {
                            var textLang = DetectLanguage(item.Text);
                            _logger?.LogInformation("Speaking from queue: {Text}, Key: {SpeakKey}, Speed: {Speed}, lang={Lang}",
                                item.Text, item.SpeakKey, _ttsConfig.Speed, textLang);
                            await _ttsService?.SpeakAsync(item.Text, textLang, speed: _ttsConfig.Speed, cancellationToken: linkedCts.Token);

                            if (!linkedCts.Token.IsCancellationRequested)
                            {
                                await Task.Delay(200, linkedCts.Token);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(item.Explanation) && !linkedCts.Token.IsCancellationRequested)
                        {
                            var explLang = DetectLanguage(item.Explanation);
                            _logger?.LogInformation("Speaking explanation: {Explanation}, Key: {SpeakKey}, Speed: {Speed}, lang={Lang}",
                                item.Explanation, item.SpeakKey, _ttsConfig.Speed, explLang);
                            await _ttsService?.SpeakAsync(item.Explanation, explLang, speed: _ttsConfig.Speed, cancellationToken: linkedCts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger?.LogDebug("Speech cancelled for key: {SpeakKey}", item.SpeakKey);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to speak for key: {SpeakKey}", item.SpeakKey);
                    }
                    finally
                    {
                        UpdateSpeakState(item.SpeakKey, false);
                    }
                }
                
                linkedCts.Dispose();
            }
            catch (OperationCanceledException)
            {
                _logger?.LogDebug("ProcessQueueAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to process speech queue");
            }
            finally
            {
                Interlocked.Exchange(ref _isProcessingQueue, 0);
            }
        }

        private void UpdateSpeakState(string speakKey, bool isSpeaking)
        {
            _currentSpeakKey = speakKey;
            SpeakStateChanged?.Invoke(this, new SpeakStateChangedEventArgs(speakKey, isSpeaking));
        }

        private static string DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "en";

            foreach (char c in text)
            {
                if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF))
                    return "zh";
            }
            return "en";
        }

        private record SpeechQueueItem(string Text, string Language, string? Explanation, string SpeakKey);
    }
}
