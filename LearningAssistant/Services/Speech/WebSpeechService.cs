using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace LearningAssistant.Services.Speech
{
    /// <summary>
    /// Web Speech API 服务实现
    /// 通过WebView2承载JavaScript实现语音识别和语音合成
    /// </summary>
    public class WebSpeechService : IWebSpeechService, IDisposable
    {
        private readonly ILogger<WebSpeechService>? _logger;
        private readonly List<string> _speechJsFunctions = new();
        private Microsoft.Web.WebView2.WinForms.WebView2? _webView;

        public event EventHandler<SpeechRecognitionResult>? RecognitionResult;
        public event EventHandler? RecognitionStarted;
        public event EventHandler? RecognitionEnded;

        private bool _isListening;
        private CancellationTokenSource? _recognitionCts;
        private EventHandler<SpeechRecognitionResult>? _continuousResultHandler;

        public bool IsRecognitionSupported => true;
        public bool IsListening => _isListening;

        public WebSpeechService(ILogger<WebSpeechService>? logger = null)
        {
            _logger = logger;

            RegisterSpeechFunctions();
        }

        public async Task SetWebViewAsync(Microsoft.Web.WebView2.WinForms.WebView2 webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            
            if (_webView.CoreWebView2 == null)
                throw new InvalidOperationException("WebView2未初始化");

            foreach (var func in _speechJsFunctions)
            {
                await _webView.CoreWebView2.ExecuteScriptAsync(func);
            }

            _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        }

        [Obsolete("Use SetWebViewAsync instead to properly handle initialization errors")]
        public void SetWebView(Microsoft.Web.WebView2.WinForms.WebView2 webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            _ = SetWebViewAsync(webView).ContinueWith(t => 
            {
                if (t.Exception != null)
                {
                    _logger?.LogError(t.Exception, "WebView2初始化失败");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = System.Text.Json.JsonSerializer.Deserialize<SpeechWebMessage>(e.WebMessageAsJson);
                if (message?.Type == "recognitionStart")
                {
                    RecognitionStarted?.Invoke(this, EventArgs.Empty);
                }
                else if (message?.Type == "recognitionEnd")
                {
                    var result = new SpeechRecognitionResult();
                    if (message.Result is System.Text.Json.JsonElement element)
                    {
                        if (element.TryGetProperty("text", out var textElement))
                            result.Text = textElement.GetString() ?? "";
                        if (element.TryGetProperty("confidence", out var confElement))
                            result.Confidence = confElement.GetDouble();
                        if (element.TryGetProperty("error", out var errElement))
                            result.Error = errElement.GetString();
                    }
                    OnRecognitionResult(result);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "解析语音消息失败");
            }
        }

        #region JavaScript函数注册

        private void RegisterSpeechFunctions()
        {
            // 语音合成函数
            _speechJsFunctions.Add(@"
                window.speakText = function(text, lang, rate) {
                    return new Promise((resolve, reject) => {
                        if (!window.speechSynthesis) {
                            reject('Speech synthesis not supported');
                            return;
                        }
                        
                        const utterance = new SpeechSynthesisUtterance(text);
                        utterance.lang = lang || 'zh-CN';
                        utterance.rate = rate || 1.0;
                        
                        utterance.onend = () => resolve();
                        utterance.onerror = (e) => reject(e.error);
                        
                        window.speechSynthesis.cancel();
                        window.speechSynthesis.speak(utterance);
                    });
                };
                
                window.stopSpeaking = function() {
                    if (window.speechSynthesis) {
                        window.speechSynthesis.cancel();
                    }
                };
            ");

            // 语音识别函数
            _speechJsFunctions.Add(@"
                window.recognitionResult = null;
                window.recognitionCallback = null;
                window.isListening = false;
                
                window.startRecognition = function(lang) {
                    return new Promise((resolve, reject) => {
                        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
                        
                        if (!SpeechRecognition) {
                            reject('Speech recognition not supported');
                            return;
                        }
                        
                        const recognition = new SpeechRecognition();
                        recognition.lang = lang || 'zh-CN';
                        recognition.continuous = false;
                        recognition.interimResults = false;
                        recognition.maxAlternatives = 1;
                        
                        recognition.onresult = (event) => {
                            const result = event.results[0][0];
                            window.recognitionResult = {
                                text: result.transcript,
                                confidence: result.confidence
                            };
                        };
                        
                        recognition.onend = () => {
                            window.isListening = false;
                            if (window.chrome && window.chrome.webview) {
                                window.chrome.webview.postMessage({
                                    type: 'recognitionEnd',
                                    result: window.recognitionResult
                                });
                            }
                        };
                        
                        recognition.onerror = (event) => {
                            window.isListening = false;
                            window.recognitionResult = {
                                error: event.error
                            };
                        };
                        
                        window.recognitionCallback = resolve;
                        window.recognition = recognition;
                        window.isListening = true;
                        
                        if (window.chrome && window.chrome.webview) {
                            window.chrome.webview.postMessage({ type: 'recognitionStart' });
                        }
                        
                        recognition.start();
                    });
                };
                
                window.stopRecognition = function() {
                    if (window.recognition) {
                        window.recognition.stop();
                        window.isListening = false;
                    }
                };
                
                window.startContinuousRecognition = function(lang) {
                    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
                    
                    if (!SpeechRecognition) {
                        throw new Error('Speech recognition not supported');
                    }
                    
                    if (window.recognition) {
                        window.recognition.stop();
                    }
                    
                    const recognition = new SpeechRecognition();
                    recognition.lang = lang || 'zh-CN';
                    recognition.continuous = true;
                    recognition.interimResults = false;
                    recognition.maxAlternatives = 1;
                    
                    recognition.onresult = (event) => {
                        const result = event.results[event.results.length - 1][0];
                        if (window.chrome && window.chrome.webview) {
                            window.chrome.webview.postMessage({
                                type: 'recognitionEnd',
                                result: {
                                    text: result.transcript,
                                    confidence: result.confidence
                                }
                            });
                        }
                    };
                    
                    recognition.onend = () => {
                        if (window.isListening) {
                            try {
                                recognition.start();
                            } catch (e) {
                                console.error('Restart recognition failed:', e);
                            }
                        }
                    };
                    
                    recognition.onerror = (event) => {
                        console.error('Recognition error:', event.error);
                        if (event.error !== 'no-speech') {
                            window.isListening = false;
                        }
                    };
                    
                    window.recognition = recognition;
                    window.isListening = true;
                    
                    if (window.chrome && window.chrome.webview) {
                        window.chrome.webview.postMessage({ type: 'recognitionStart' });
                    }
                    
                    recognition.start();
                };
            ");

            // 获取识别结果函数
            _speechJsFunctions.Add(@"
                window.getRecognitionResult = function() {
                    return window.recognitionResult || { text: '', confidence: 0 };
                };
                
                window.getIsListening = function() {
                    return window.isListening;
                };
            ");
        }

        #endregion

        #region 语音合成

        /// <summary>
        /// 语音合成 - 朗读文本
        /// </summary>
        public async Task SpeakAsync(string text, string language = "zh-CN", float rate = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            _logger?.LogDebug("开始语音合成: 语言={Lang}, 文本长度={Length}", language, text.Length);

            try
            {
                // 构建JavaScript调用
                var escapedText = text.Replace("'", "\\'").Replace("\n", " ");
                var script = $"window.speakText('{escapedText}', '{language}', {rate})";

                // 在UI线程执行（WebView需要在UI线程访问）
                await ExecuteScriptAsync(script);

                _logger?.LogDebug("语音合成完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "语音合成失败");
            }
        }

        /// <summary>
        /// 停止语音合成
        /// </summary>
        public void StopSpeaking()
        {
            try
            {
                ExecuteScriptSync("window.stopSpeaking()");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "停止语音合成失败");
            }
        }

        #endregion

        #region 语音识别

        /// <summary>
        /// 单次语音识别
        /// </summary>
        public async Task<SpeechRecognitionResult> RecognizeOnceAsync(string language = "zh-CN")
        {
            var result = new SpeechRecognitionResult();

            try
            {
                _logger?.LogDebug("开始语音识别: 语言={Lang}", language);

                // 确保不在识别中
                if (_isListening)
                {
                    await StopContinuousRecognitionAsync();
                }

                _recognitionCts = new CancellationTokenSource();
                _isListening = true;
                RecognitionStarted?.Invoke(this, EventArgs.Empty);

                // 调用JavaScript开始识别
                var script = $"window.startRecognition('{language}')";
                await ExecuteScriptAsync(script);

                // 等待识别结果（通过事件返回）
                var timeout = Task.Delay(30000, _recognitionCts.Token);
                var completionSource = new TaskCompletionSource<bool>();

                void OnResult(object? sender, SpeechRecognitionResult e)
                {
                    completionSource.TrySetResult(true);
                }

                RecognitionResult += OnResult;

                try
                {
                    await Task.WhenAny(completionSource.Task, timeout);
                    _recognitionCts.Cancel();
                }
                finally
                {
                    RecognitionResult -= OnResult;
                }
            }
            catch (OperationCanceledException)
            {
                result.Text = "";
                result.Error = "识别超时";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "语音识别失败");
                result.Text = "";
                result.Error = ex.Message;
            }
            finally
            {
                _isListening = false;
                RecognitionEnded?.Invoke(this, EventArgs.Empty);
            }

            return result;
        }

        /// <summary>
        /// 开始连续语音识别
        /// </summary>
        public async Task StartContinuousRecognitionAsync(Action<string> onResult, string language = "zh-CN")
        {
            if (_isListening)
            {
                _logger?.LogWarning("已经在监听中");
                return;
            }

            if (_webView?.CoreWebView2 == null)
            {
                _logger?.LogError("WebView2未初始化，无法开始连续识别");
                throw new InvalidOperationException("WebView2未初始化");
            }

            _isListening = true;
            RecognitionStarted?.Invoke(this, EventArgs.Empty);

            _logger?.LogInformation("开始连续语音识别: 语言={Lang}", language);

            try
            {
                var script = $"window.startContinuousRecognition('{language}')";
                await ExecuteScriptAsync(script);

                void OnContinuousResult(object? sender, SpeechRecognitionResult result)
                {
                    if (!string.IsNullOrEmpty(result.Text))
                    {
                        onResult?.Invoke(result.Text);
                    }
                }

                RecognitionResult += OnContinuousResult;

                _continuousResultHandler = OnContinuousResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "启动连续语音识别失败");
                _isListening = false;
                RecognitionEnded?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }

        /// <summary>
        /// 停止连续语音识别
        /// </summary>
        public Task StopContinuousRecognitionAsync()
        {
            if (!_isListening)
                return Task.CompletedTask;

            try
            {
                ExecuteScriptSync("window.stopRecognition()");
                _recognitionCts?.Cancel();

                if (_continuousResultHandler != null)
                {
                    RecognitionResult -= _continuousResultHandler;
                    _continuousResultHandler = null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "停止语音识别失败");
            }
            finally
            {
                _isListening = false;
                RecognitionEnded?.Invoke(this, EventArgs.Empty);
            }

            return Task.CompletedTask;
        }

        #endregion

        public void OnRecognitionResult(SpeechRecognitionResult result)
        {
            RecognitionResult?.Invoke(this, result);
        }

        #region 辅助方法

        private async Task ExecuteScriptAsync(string script)
        {
            if (_webView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView2未初始化");

            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private void ExecuteScriptSync(string script)
        {
            if (_webView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView2未初始化");

            _webView.CoreWebView2.ExecuteScriptAsync(script).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 获取所有JavaScript函数
        /// </summary>
        public IReadOnlyList<string> GetSpeechFunctions()
        {
            return _speechJsFunctions.AsReadOnly();
        }

        #endregion

        public void Dispose()
        {
            _recognitionCts?.Cancel();
            _recognitionCts?.Dispose();
            if (_webView?.CoreWebView2 != null)
            {
                _webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            }
        }
    }

    /// <summary>
    /// WebView扩展方法，用于执行语音脚本
    /// </summary>
    public static class WebSpeechServiceExtensions
    {
        /// <summary>
        /// 在WebView中初始化语音服务
        /// </summary>
        public static async Task InitializeSpeechServiceAsync(this Microsoft.Web.WebView2.WinForms.WebView2 webView, WebSpeechService service)
        {
            if (webView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView2未初始化");

            // 注入JavaScript函数
            foreach (var func in service.GetSpeechFunctions())
            {
                await webView.CoreWebView2.ExecuteScriptAsync(func);
            }

            // 设置消息处理
            webView.CoreWebView2.WebMessageReceived += (sender, e) =>
            {
                try
                {
                    var message = System.Text.Json.JsonSerializer.Deserialize<SpeechWebMessage>(e.WebMessageAsJson);
                    if (message?.Type == "recognitionStart")
                    {
                        // 识别开始
                    }
                    else if (message?.Type == "recognitionEnd")
                    {
                        // 识别结束
                        var result = new SpeechRecognitionResult();
                        if (message.Result is System.Text.Json.JsonElement element)
                        {
                            if (element.TryGetProperty("text", out var textElement))
                                result.Text = textElement.GetString() ?? "";
                            if (element.TryGetProperty("confidence", out var confElement))
                                result.Confidence = confElement.GetDouble();
                            if (element.TryGetProperty("error", out var errElement))
                                result.Error = errElement.GetString();
                        }
                        service.OnRecognitionResult(result);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError($"解析语音消息失败: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// 在WebView中执行脚本
        /// </summary>
        public static async Task ExecuteScriptAsync(this Microsoft.Web.WebView2.WinForms.WebView2 webView, string script)
        {
            if (webView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView2未初始化");

            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }
    }

    internal class SpeechWebMessage
    {
        public string? Type { get; set; }
        public object? Result { get; set; }
    }
}
