namespace Trading.Infras.Data.Configuration;

/// <summary>
/// OANDA API配置
/// 文档: https://developer.oanda.com/rest-live-v20/introduction/
/// </summary>
public class OandaSettings
{
    /// <summary>
    /// API密钥
    /// 从OANDA账户管理页面生成
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 账户ID
    /// 格式: XXX-XXX-XXXXXXXX-XXX
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// API基础URL
    /// Practice (模拟): https://api-fxpractice.oanda.com
    /// Live (实盘): https://api-fxtrade.oanda.com
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api-fxpractice.oanda.com";

    /// <summary>
    /// Streaming API基础URL
    /// Practice (模拟): https://stream-fxpractice.oanda.com
    /// Live (实盘): https://stream-fxtrade.oanda.com
    /// </summary>
    public string StreamingBaseUrl { get; set; } = "https://stream-fxpractice.oanda.com";

    /// <summary>
    /// 环境类型 (practice/live)
    /// </summary>
    public string Environment { get; set; } = "practice";
}
