using System.Media;
using LearningAssistant.Services.TTS;

namespace LearningAssistant.Services.Feedback
{
    public class SoundService : ISoundService
    {
        private readonly ITTSService? _ttsService;
        private static readonly string CacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SoundCache");
        private static readonly string HoorayCachePath = Path.Combine(CacheDirectory, "hooray.wav");
        private static readonly string OopsCachePath = Path.Combine(CacheDirectory, "oops.wav");
        private static readonly string NavigationCachePath = Path.Combine(CacheDirectory, "nav.wav");
        private static readonly string AchievementCachePath = Path.Combine(CacheDirectory, "achievement.wav");
        private static readonly string ClickCachePath = Path.Combine(CacheDirectory, "click.wav");

        public SoundService(ITTSService? ttsService = null)
        {
            _ttsService = ttsService;
            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
        }
        
        public void PlaySuccess()
        {
            PlayCachedTtsSound("Hooray!", HoorayCachePath);
        }
        
        public void PlayError()
        {
            PlayCachedTtsSound("Oops!", OopsCachePath);
        }
        
        public void PlayNavigation()
        {
            try
            {
                SystemSounds.Beep.Play();
            }
            catch
            {
            }
        }
        
        public void PlayAchievement()
        {
            PlayCachedTtsSound("Great job!", AchievementCachePath);
        }
        
        public void PlayClick()
        {
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch
            {
            }
        }

        private async void PlayCachedTtsSound(string text, string cachePath)
        {
            try
            {
                if (File.Exists(cachePath))
                {
                    using (var player = new SoundPlayer(cachePath))
                    {
                        player.Play();
                    }
                    return;
                }

                if (_ttsService != null && _ttsService.Available)
                {
                    var audioBytes = await _ttsService.SpeakSteamAsync(text, "en", 1.0f, "wav");
                    if (audioBytes != null && audioBytes.Length > 0)
                    {
                        File.WriteAllBytes(cachePath, audioBytes);
                        using (var player = new MemoryStream(audioBytes))
                        using (var soundPlayer = new SoundPlayer(player))
                        {
                            soundPlayer.Play();
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}