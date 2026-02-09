using Microsoft.AspNetCore.Mvc;
using Trading.Infrastructure.Configuration;

namespace Trading.Web.Controllers;

/// <summary>
/// 配置诊断API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly TelegramSettings _telegramSettings;
    private readonly TradeLockerSettings _tradeLockerSettings;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        TelegramSettings telegramSettings,
        TradeLockerSettings tradeLockerSettings,
        ILogger<ConfigController> logger)
    {
        _telegramSettings = telegramSettings;
        _tradeLockerSettings = tradeLockerSettings;
        _logger = logger;
    }

    /// <summary>
    /// 检查配置状态（不显示敏感信息）
    /// </summary>
    [HttpGet("status")]
    public ActionResult GetStatus()
    {
        return Ok(new
        {
            telegram = new
            {
                enabled = _telegramSettings.Enabled,
                botTokenConfigured = !string.IsNullOrEmpty(_telegramSettings.BotToken),
                botTokenLength = _telegramSettings.BotToken?.Length ?? 0,
                chatIdConfigured = _telegramSettings.DefaultChatId.HasValue,
                chatId = _telegramSettings.DefaultChatId,
                isDemo = string.IsNullOrEmpty(_telegramSettings.BotToken)
            },
            tradeLocker = new
            {
                environment = _tradeLockerSettings.Environment,
                emailConfigured = !string.IsNullOrEmpty(_tradeLockerSettings.Email),
                passwordConfigured = !string.IsNullOrEmpty(_tradeLockerSettings.Password),
                serverConfigured = !string.IsNullOrEmpty(_tradeLockerSettings.Server),
                accountIdConfigured = _tradeLockerSettings.AccountId > 0,
                isDemo = string.IsNullOrEmpty(_tradeLockerSettings.Email)
            }
        });
    }
}
