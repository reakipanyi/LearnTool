using System.Runtime.InteropServices;
using System.Media;

namespace UnifiedLearningAssistant.Services.Feedback
{
    public class SoundService : ISoundService
    {
        [DllImport("kernel32.dll")]
        private static extern bool Beep(int frequency, int duration);
        
        private readonly bool _soundEnabled = true;
        
        public void PlaySuccess()
        {
            if (!_soundEnabled) return;
            
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch
            {
                PlayBeep(800, 150);
            }
        }
        
        public void PlayError()
        {
            if (!_soundEnabled) return;
            
            try
            {
                SystemSounds.Hand.Play();
            }
            catch
            {
                PlayBeep(300, 200);
            }
        }
        
        public void PlayNavigation()
        {
            if (!_soundEnabled) return;
            
            try
            {
                SystemSounds.Beep.Play();
            }
            catch
            {
                PlayBeep(600, 80);
            }
        }
        
        public void PlayAchievement()
        {
            if (!_soundEnabled) return;
            
            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch
            {
                PlayTriumph();
            }
        }
        
        public void PlayClick()
        {
            if (!_soundEnabled) return;
            
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch
            {
                PlayBeep(500, 50);
            }
        }
        
        private void PlayBeep(int frequency, int duration)
        {
            try
            {
                Beep(frequency, duration);
            }
            catch
            {
            }
        }
        
        private void PlayTriumph()
        {
            try
            {
                Beep(523, 100);
                Thread.Sleep(50);
                Beep(659, 100);
                Thread.Sleep(50);
                Beep(784, 150);
            }
            catch
            {
            }
        }
    }
}
