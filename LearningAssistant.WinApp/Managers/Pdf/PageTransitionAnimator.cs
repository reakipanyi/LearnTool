using LearningAssistant.Abstractions;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    public class PageTransitionAnimator
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;
        private bool _isAnimating = false;
        private int _transitionStep = 0;
        private bool _transitionFadeOut = false;

        public PageTransitionAnimator(ILogger logger, IPdfReaderFormAccess form)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
        }

        public bool IsAnimating => _isAnimating;

        public void StartPageTransition(bool forward)
        {
            if (_isAnimating || _form.PageTransitionOverlay == null) return;

            _isAnimating = true;
            _transitionStep = 0;
            _transitionFadeOut = true;

            bool isNightMode = _form.IsNightMode;
            _form.PageTransitionOverlay.Visible = true;
            _form.PageTransitionOverlay.BackColor = isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;

            if (_form.PageTransitionTimer != null)
            {
                _form.PageTransitionTimer.Interval = 25;
                _form.PageTransitionTimer.Start();
            }
        }

        public void OnPageTransitionTick()
        {
            try
            {
                if (_form.PageTransitionOverlay == null || !_isAnimating) return;

                _transitionStep++;

                bool isNightMode = _form.IsNightMode;
                int baseR = isNightMode ? 30 : 255;
                int baseG = isNightMode ? 30 : 255;
                int baseB = isNightMode ? 30 : 255;

                if (_transitionFadeOut)
                {
                    int alpha = 255 - (_transitionStep * 30);
                    if (alpha <= 0)
                    {
                        alpha = 0;
                        _transitionFadeOut = false;
                        _transitionStep = 0;
                    }
                    _form.PageTransitionOverlay.BackColor = Color.FromArgb(alpha, baseR, baseG, baseB);
                }
                else
                {
                    int alpha = _transitionStep * 30;
                    if (alpha >= 255)
                    {
                        alpha = 255;
                        _form.PageTransitionTimer?.Stop();
                        _isAnimating = false;
                        _form.PageTransitionOverlay.Visible = false;
                        return;
                    }
                    _form.PageTransitionOverlay.BackColor = Color.FromArgb(alpha, baseR, baseG, baseB);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in page transition animation");
                _isAnimating = false;
                _form.PageTransitionTimer?.Stop();
                if (_form.PageTransitionOverlay != null)
                    _form.PageTransitionOverlay.Visible = false;
            }
        }
    }
}