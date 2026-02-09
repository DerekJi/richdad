namespace Trading.Infrastructure.Configuration;

/// <summary>
/// TradeLocker API配置
/// </summary>
public class TradeLockerSettings
{
    /// <summary>
    /// 环境类型：demo或live
    /// </summary>
    public string Environment { get; set; } = "demo";

    /// <summary>
    /// API基础URL（根据环境自动设置）
    /// demo环境用于测试，live环境用于实盘交易
    /// </summary>
    public string ApiBaseUrl
    {
        get
        {
            var env = Environment.ToLower();
            if (env == "live")
                return "https://live.tradelocker.com";
            else
                return "https://demo.tradelocker.com";
        }
    }

    /// <summary>
    /// 账户邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 账户密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 服务器名称（登录TradeLocker时连接的服务器）
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// 账户ID（从TradeLocker平台获取，格式如：123456）
    /// </summary>
    public long AccountId { get; set; }

    /// <summary>
    /// 账户编号（accNum，通常是1或2等单个数字）
    /// </summary>
    public int AccountNumber { get; set; } = 1;

    /// <summary>
    /// 开发者API密钥（可选，用于提高速率限制）
    /// </summary>
    public string? DeveloperApiKey { get; set; }
}
