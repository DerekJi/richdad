namespace Trading.AlertSystem.Data.Configuration;

/// <summary>
/// TradeLocker API配置
/// </summary>
public class TradeLockerSettings
{
    /// <summary>
    /// API基础URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.tradelocker.com";

    /// <summary>
    /// API访问令牌
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// 账户用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 账户密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 服务器标识
    /// </summary>
    public string? Server { get; set; }

    /// <summary>
    /// 账户ID
    /// </summary>
    public long? AccountId { get; set; }
}
