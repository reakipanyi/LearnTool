
using LearningAssistant.Abstractions;
using LearningAssistant.Common.Events;

namespace LearningAssistant.Services.Notifications
{
    public class NotificationService : INotificationService, ICelebrationService, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly Form _mainForm;
        private readonly IDialogService? _dialogService;

        public NotificationService(IEventBus eventBus, Form mainForm, IDialogService? dialogService = null)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            _dialogService = dialogService;
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _eventBus.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
            _eventBus.Subscribe<LearningSessionCompletedEvent>(OnLearningSessionCompleted);
        }

        private void UnsubscribeFromEvents()
        {
            _eventBus.Unsubscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);
            _eventBus.Unsubscribe<LearningSessionCompletedEvent>(OnLearningSessionCompleted);
        }

        private void OnAchievementUnlocked(AchievementUnlockedEvent evt)
        {
            ShowAchievementUnlock(evt.Icon, $"成就解锁: {evt.AchievementName}", evt.Description);
            TriggerCelebration(CelebrationType.AchievementUnlocked, new CelebrationContext
            {
                UserId = evt.UserId,
                AchievementId = evt.AchievementId
            });
        }

        private void OnLearningSessionCompleted(LearningSessionCompletedEvent evt)
        {
            if (evt.Accuracy >= 0.95)
            {
                TriggerCelebration(CelebrationType.PerfectScore, new CelebrationContext
                {
                    UserId = evt.UserId,
                    Score = (int)(evt.Accuracy * 100)
                });
            }
        }

        public void ShowNotification(Notification notification)
        {
            // 这里可以实现通知显示逻辑
            // 例如使用 Toast 通知或者自定义窗体
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new Action(() => ShowNotificationInternal(notification)));
            }
            else
            {
                ShowNotificationInternal(notification);
            }
        }

        private void ShowNotificationInternal(Notification notification)
        {
            if (_dialogService != null)
                _dialogService.ShowMessageAsync(notification.Type.ToString(), $"{notification.Title}\n\n{notification.Message}").GetAwaiter().GetResult();
            else
                MessageBox.Show($"{notification.Title}\n\n{notification.Message}",
                    notification.Type.ToString(),
                    MessageBoxButtons.OK,
                    GetIcon(notification.Type));
        }

        public void ShowAchievementUnlock(string icon, string title, string message)
        {
            ShowNotification(new Notification
            {
                Title = title,
                Message = message,
                Icon = icon,
                Type = NotificationType.Achievement,
                Duration = TimeSpan.FromSeconds(5)
            });
        }

        public void ShowLearningMilestone(string message)
        {
            ShowNotification(new Notification
            {
                Title = "学习里程碑",
                Message = message,
                Type = NotificationType.Success,
                Duration = TimeSpan.FromSeconds(4)
            });
        }

        public void ShowError(string title, string message)
        {
            ShowNotification(new Notification
            {
                Title = title,
                Message = message,
                Type = NotificationType.Error,
                Duration = TimeSpan.FromSeconds(6)
            });
        }

        public void TriggerCelebration(CelebrationType type, CelebrationContext context)
        {
            // 根据庆祝类型触发不同的庆祝方式
            switch (type)
            {
                case CelebrationType.AchievementUnlocked:
                    ShowConfetti();
                    break;
                case CelebrationType.PerfectScore:
                    ShowConfetti();
                    break;
                case CelebrationType.LearningComplete:
                case CelebrationType.MilestoneReached:
                    // 可以播放不同的音效或动画
                    break;
            }
        }

        public void ShowConfetti()
        {
            // 这里可以实现彩纸效果的显示
            // 可以创建一个临时窗体显示彩纸动画
            // 为了简单，这里只做一个占位实现
        }

        public void PlayCelebrationSound()
        {
            // 这里可以实现播放庆祝音效
        }

        private static MessageBoxIcon GetIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => MessageBoxIcon.Information,
                NotificationType.Warning => MessageBoxIcon.Warning,
                NotificationType.Error => MessageBoxIcon.Error,
                NotificationType.Achievement => MessageBoxIcon.Asterisk,
                _ => MessageBoxIcon.Information
            };
        }

        public void Dispose()
        {
            UnsubscribeFromEvents();
        }
    }
}

