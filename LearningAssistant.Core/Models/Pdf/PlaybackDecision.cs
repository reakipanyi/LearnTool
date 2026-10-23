namespace LearningAssistant.Models.Pdf
{
    /// <summary>播放范围：跟随翻页切换，或整篇/整本只用一个音轨。</summary>
    public enum PlaybackScope
    {
        FollowPage,
        WholeBook
    }

    /// <summary>播放器的六个主状态 + 试听子状态；翻页判断只依赖它。</summary>
    public enum PdfPlayerState
    {
        Idle,
        Loading,
        Playing,
        PausedByUser,
        Ended,
        Previewing,
        Error
    }

    /// <summary>翻页时播放器应执行的动作。</summary>
    public enum PlaybackAction
    {
        /// <summary>不动作。</summary>
        None,
        /// <summary>只换显示，不播放（暂停态翻页）。</summary>
        LoadOnly,
        /// <summary>淡出 → Load → 淡入并从头播放。</summary>
        LoadAndPlay,
        /// <summary>暂停并提示“本页无音频 / 文件缺失”。</summary>
        StopWithHint,
        /// <summary>退出试听，恢复原曲与进度。</summary>
        PreviewExit
    }

    /// <summary>
    /// 翻页行为的唯一判定表。纯函数、无副作用，矩阵每一行都可以直接写单测覆盖。
    /// </summary>
    public static class PlaybackDecision
    {
        public static PlaybackAction Resolve(
            PlaybackScope scope,
            bool follow,
            PdfPlayerState playerState,
            string? currentPath,
            string? targetPath)
        {
            // 整篇播放时翻页永不动作，杜绝一切重启
            if (scope == PlaybackScope.WholeBook) return PlaybackAction.None;
            // 跟随翻页关闭时同样完全不动作
            if (!follow) return PlaybackAction.None;
            // 试听中翻页不切音频，试听结束后由调用方恢复原曲
            if (playerState == PdfPlayerState.Previewing) return PlaybackAction.None;

            var target = Normalize(targetPath);
            if (target == null) return PlaybackAction.StopWithHint;

            var current = Normalize(currentPath);
            // 唯一切换条件是规范化路径不同，而不是页码变化
            if (current != null && string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
                return PlaybackAction.None;

            return playerState switch
            {
                PdfPlayerState.Playing => PlaybackAction.LoadAndPlay,
                PdfPlayerState.Loading => PlaybackAction.LoadAndPlay,
                PdfPlayerState.PausedByUser => PlaybackAction.LoadOnly,
                PdfPlayerState.Ended => PlaybackAction.LoadOnly,
                PdfPlayerState.Error => PlaybackAction.LoadOnly,
                _ => PlaybackAction.None
            };
        }

        /// <summary>规范化路径：展开为完整路径；空值返回 null。</summary>
        public static string? Normalize(string? path)
            => string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
    }
}
