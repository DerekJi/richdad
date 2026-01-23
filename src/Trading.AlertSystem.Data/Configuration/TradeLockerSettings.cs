namespace Trading.AlertSystem.Data.Configuration;

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
    /// API基础URL（自动根据环境设置）
    /// demo: https://demo.tradelocker.com/backend-api/
    /// live: https://live.tradelocker.com/backend-api/
    /// </summary>
    public string ApiBaseUrl => Environment.ToLower() == "live"
        ? "https://live.tradelocker.com/backend-api"
        : "https://demo.tradelocker.com/backend-api";

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
