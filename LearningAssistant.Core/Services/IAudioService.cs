namespace LearningAssistant.Services
{
    /// <summary>
    /// 音频服务接口 - 提供音频播放控制功能（基于VLC）
    /// 实现类需要继承IDisposable以确保VLC资源正确释放
    /// </summary>
    public interface IAudioService : IDisposable
    {
        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 是否正在播放
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// 初始化音频服务
        /// </summary>
        /// <param name="vlcLibPath">VLC库文件路径（libvlc.dll所在目录）</param>
        void Initialize(string vlcLibPath);

        /// <summary>
        /// 设置要播放的媒体文件
        /// </summary>
        /// <param name="mediaUri">媒体文件的URI</param>
        void SetMedia(Uri mediaUri);

        /// <summary>
        /// 开始播放
        /// </summary>
        void Play();

        /// <summary>
        /// 暂停播放
        /// </summary>
        void Pause();

        /// <summary>
        /// 停止播放
        /// </summary>
        void Stop();

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="vol">音量值（通常0-100）</param>
        void SetVolume(int vol);

        /// <summary>
        /// 设置播放速率
        /// </summary>
        /// <param name="rate">播放速率（如1.0为正常速度，2.0为2倍速）</param>
        void SetRate(float rate);

        /// <summary>
        /// 获取媒体总时长（毫秒）
        /// </summary>
        /// <returns>总时长（毫秒）</returns>
        int GetLengthMilliseconds();

        /// <summary>
        /// 获取当前播放位置（毫秒）
        /// </summary>
        /// <returns>当前位置（毫秒）</returns>
        int GetPositionMilliseconds();

        /// <summary>
        /// 设置播放位置（毫秒）
        /// </summary>
        /// <param name="ms">目标位置（毫秒）</param>
        void SetPositionByMilliseconds(int ms);
    }
}
