namespace LearningAssistant.Models.Config
{
    public class CloudStorageConfig
    {
        public string BaiduClientId { get; set; } = string.Empty;
        public string BaiduClientSecret { get; set; } = string.Empty;
        public string BaiduAccessToken { get; set; } = string.Empty;
        public string BaiduRefreshToken { get; set; } = string.Empty;
        public DateTime? BaiduTokenExpireTime { get; set; }
    }
}