using System.ComponentModel;

namespace LearningAssistant.Presenters
{
    public interface ILearningEventMediator
    {
        event EventHandler<MarkAsKnownEventArgs>? MarkAsKnown;
        event EventHandler<MarkAsUnknownEventArgs>? MarkAsUnknown;
        event EventHandler? Pronounce;
        event EventHandler? Next;
        event EventHandler? Exit;
        event EventHandler<SendToPdfEventArgs>? SendToPdfQuestion;
        event EventHandler? SettingsChanged;
        event EventHandler? OpenStatistics;
        event EventHandler? ExportErrorBook;

        void RaiseMarkAsKnown();
        void RaiseMarkAsUnknown();
        void RaisePronounce();
        void RaiseNext();
        void RaiseExit();
        void RaiseSendToPdfQuestion(string text, string language);
        void RaiseSettingsChanged();
        void RaiseOpenStatistics();
        void RaiseExportErrorBook();
    }

    public class MarkAsKnownEventArgs : EventArgs
    {
    }

    public class MarkAsUnknownEventArgs : EventArgs
    {
    }

    public class LearningEventMediator : ILearningEventMediator
    {
        public event EventHandler<MarkAsKnownEventArgs>? MarkAsKnown;
        public event EventHandler<MarkAsUnknownEventArgs>? MarkAsUnknown;
        public event EventHandler? Pronounce;
        public event EventHandler? Next;
        public event EventHandler? Exit;
        public event EventHandler<SendToPdfEventArgs>? SendToPdfQuestion;
        public event EventHandler? SettingsChanged;
        public event EventHandler? OpenStatistics;
        public event EventHandler? ExportErrorBook;

        public void RaiseMarkAsKnown()
        {
            MarkAsKnown?.Invoke(this, new MarkAsKnownEventArgs());
        }

        public void RaiseMarkAsUnknown()
        {
            MarkAsUnknown?.Invoke(this, new MarkAsUnknownEventArgs());
        }

        public void RaisePronounce()
        {
            Pronounce?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseNext()
        {
            Next?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseExit()
        {
            Exit?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseSendToPdfQuestion(string text, string language)
        {
            SendToPdfQuestion?.Invoke(this, new SendToPdfEventArgs { Text = text, Language = language });
        }

        public void RaiseSettingsChanged()
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseOpenStatistics()
        {
            OpenStatistics?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseExportErrorBook()
        {
            ExportErrorBook?.Invoke(this, EventArgs.Empty);
        }
    }
}