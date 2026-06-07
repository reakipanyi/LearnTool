using LearningAssistant.Services;

namespace LearningAssistant.Presenters
{
    public class AudioPresenter : IDisposable
    {
        //private readonly IAudioView _view;
        private readonly IAudioService _service;

        public AudioPresenter(/*IAudioView view, */IAudioService service)
        {
            //_view = view ?? throw new ArgumentNullException(nameof(view));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public void Initialize(string vlcLibPath)
        {
            try
            {
                _service.Initialize(vlcLibPath);
            }
            catch
            {
                //_view.ShowAudioError(ex.Message);
            }
        }

        public void PlayFile(string filePath)
        {
            if (!_service.IsInitialized)
            {
                //_view.ShowAudioError("Audio service is not initialized. Call Initialize with the VLC lib path before playing audio.");
                return;
            }
            try
            {
                _service.SetMedia(new Uri(filePath));
                _service.Play();
                //_view.SetCurrentFileLabel(System.IO.Path.GetFileName(filePath));
                //_view.SetPlayButtonText("暂停");
            }
            catch
            {
                //_view.ShowAudioError(ex.Message);
            }
        }

        public void TogglePlayPause()
        {
            if (!_service.IsInitialized) return;
            //if (_service.IsPlaying) { _service.Pause(); _view.SetPlayButtonText("播放"); }
            //else { _service.Play(); _view.SetPlayButtonText("暂停"); }
        }

        public void Stop()
        {
            if (!_service.IsInitialized) return;
            _service.Stop();
            //_view.SetCurrentFileLabel("未选择文件");
            //_view.SetPlayButtonText("播放");
        }

        public void SetVolume(int vol)
        {
            _service.SetVolume(vol);
        }

        public void SetSpeed(double speed)
        {
            _service.SetRate((float)speed);
            //_view.SetPlaybackSpeedLabel($"当前倍速: {speed}x");
        }

        public void UpdateProgress()
        {
            var total = _service.GetLengthMilliseconds();
            var pos = _service.GetPositionMilliseconds();
            //_view.UpdateProgress(pos, total);
        }

        public void Dispose()
        {
            _service?.Dispose();
        }
    }
}
