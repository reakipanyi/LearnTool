using LearningAssistant.Common;
using LearningAssistant.Models.User;
using LearningAssistant.Services.TTS;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Text.Json;

namespace LearningAssistant.Services.Feedback
{
    public class EncouragementService : IEncouragementService
    {
        private readonly ILogger<EncouragementService> _logger;
        private readonly ITTSService _ttsService;
        private readonly EncouragementConfig _config;
        private readonly Random _random = new Random();

        public EncouragementService(ILogger<EncouragementService> logger, ITTSService ttsService)
        {
            _logger = logger;
            _ttsService = ttsService;
            _config = LoadConfig();
        }

        private EncouragementConfig LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(AppPaths.ConfigDir, "encouragement.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<EncouragementConfig>(json);
                    if (config != null)
                    {
                        _logger?.LogInformation("Loaded encouragement config from {Path}", configPath);
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load encouragement config");
            }

            return new EncouragementConfig();
        }

        public async Task PlayRandomKnownFeedbackAsync(CancellationToken cancellationToken = default)
        {
            await PlayRandomAudioAsync(_config.KnownAudios, "known", cancellationToken);
        }

        public async Task PlayRandomUnknownFeedbackAsync(CancellationToken cancellationToken = default)
        {
            await PlayRandomAudioAsync(_config.UnknownAudios, "unknown", cancellationToken);
        }

        private async Task PlayRandomAudioAsync(System.Collections.Generic.List<string> audioList, string category, CancellationToken cancellationToken)
        {
            if (audioList == null || audioList.Count == 0)
            {
                _logger?.LogWarning("Audio list is empty for category {Category}", category);
                return;
            }

            string selectedAudio = audioList[_random.Next(audioList.Count)];
            _logger?.LogInformation("Playing random {Category} audio: {Audio}", category, selectedAudio);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string audioPath = FindLocalAudioFile(selectedAudio);
                if (audioPath != null && File.Exists(audioPath))
                {
                    await Task.Run(() => PlayLocalAudioFile(audioPath), cancellationToken);
                }
                else if (_config.UseTTSAsFallback)
                {
                    await _ttsService.SpeakAsync(selectedAudio, DetectLanguage(selectedAudio));
                }
                else
                {
                    _logger?.LogWarning("No local audio found and TTS fallback is disabled for: {Audio}", selectedAudio);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to play {Category} audio: {Audio}", category, selectedAudio);
            }
        }

        private string FindLocalAudioFile(string audioName)
        {
            string[] possiblePaths = new[]
            {
                Path.Combine(AppPaths.DataDir, "Audio", $"{audioName}.wav"),
                Path.Combine(AppPaths.DataDir, "Audio", $"{audioName}.mp3"),
                Path.Combine(AppPaths.DataDir, "Audio", "known", $"{audioName}.wav"),
                Path.Combine(AppPaths.DataDir, "Audio", "known", $"{audioName}.mp3"),
                Path.Combine(AppPaths.DataDir, "Audio", "unknown", $"{audioName}.wav"),
                Path.Combine(AppPaths.DataDir, "Audio", "unknown", $"{audioName}.mp3"),
                Path.Combine(AppPaths.DataDir, "Audio", $"{audioName}.wav"),
                Path.Combine(AppPaths.DataDir, "Audio", $"{audioName}.mp3")
            };

            return possiblePaths.FirstOrDefault(File.Exists);
        }

        private void PlayLocalAudioFile(string filePath)
        {
            try
            {
                using var audioFile = new AudioFileReader(filePath);
                using var outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.Volume = _config.Volume / 100f;
                outputDevice.Play();

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to play local audio file: {Path}", filePath);
            }
        }

        private string DetectLanguage(string text)
        {
            if (string.IsNullOrEmpty(text)) return "zh-CN";

            return text.Any(c => c >= 0x4E00 && c <= 0x9FFF) ? "zh-CN" : "en-US";
        }
    }
}
