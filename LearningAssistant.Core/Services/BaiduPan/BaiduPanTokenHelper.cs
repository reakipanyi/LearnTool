namespace LearningAssistant.Services.Baidu
{
    public class BaiduPanTokenHelper
    {
        public static async Task GetToken()
        {
            string appID = "";
            string appKey = "";
            string secretkey = "";
            string signkey = "";



            // 1. 配置应用信息（从百度开放平台获取）
            var authConfig = new BaiduPanAuthCodeManager.AuthCodeConfig
            {
                ClientId = appKey,
                ClientSecret = secretkey,
                RedirectUri = "oob",
                Scope = "basic,netdisk",
                DeviceId = ""
            };

            using var authManager = new BaiduPanAuthCodeManager(authConfig);

            try
            {
                // ========== 步骤1：生成授权Code请求URL ==========
                // 可选参数：展示二维码登录、防CSRF的state参数
                string authUrl = authManager.GenerateCodeRequestUrl(
                    display: "popup",
                    state: "random_csrf_token_123456",
                    qrcode: 1,
                    forceLogin: 0);

                Console.WriteLine($"请访问以下URL完成授权：\n{authUrl}");
                Console.WriteLine("\n授权成功后，请输入回调地址返回的Code：");
                string code = Console.ReadLine().Trim();

                // ========== 步骤2：通过Code换取Access Token ==========
                var tokenResponse = await authManager.ExchangeCodeForTokenAsync(code);
                Console.WriteLine($"\n换取Token成功：");
                Console.WriteLine($"Access Token：{tokenResponse.AccessToken}");
                Console.WriteLine($"Refresh Token：{tokenResponse.RefreshToken}");
                Console.WriteLine($"有效期：{tokenResponse.ExpiresIn / 3600 / 24} 天");

                // ========== 步骤3：Token过期后刷新（模拟） ==========
                Console.WriteLine("\n模拟Token过期后刷新...");
                // 实际场景中无需手动刷新，调用GetValidAccessTokenAsync会自动刷新
                var refreshedToken = await authManager.RefreshAccessTokenAsync(tokenResponse.RefreshToken);
                Console.WriteLine($"刷新后的Access Token：{refreshedToken.AccessToken}");
                Console.WriteLine($"新的Refresh Token：{refreshedToken.RefreshToken}");

                // ========== 步骤4：获取有效AccessToken（自动处理刷新） ==========
                string validToken = await authManager.GetValidAccessTokenAsync();
                Console.WriteLine($"\n当前有效AccessToken：{validToken}");
                var accessToken1 = validToken;
            }
            catch (BaiduPanAuthCodeManager.BaiduPanAuthCodeException ex)
            {
                Console.WriteLine($"\n授权失败：错误码={ex.ErrorCode}，描述={ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n操作异常：{ex.Message}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }

}
