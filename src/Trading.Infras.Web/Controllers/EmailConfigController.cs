using Microsoft.AspNetCore.Mvc;
using Trading.Infras.Data.Configuration;
using Trading.Infras.Data.Models;
using Trading.Infras.Data.Repositories;
using Trading.Infras.Data.Services;

namespace Trading.Infras.Web.Controllers;

/// <summary>
/// 邮件配置管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmailConfigController : ControllerBase
{
    private readonly IEmailConfigRepository _repository;
    private readonly ILogger<EmailConfigController> _logger;

    public EmailConfigController(
        IEmailConfigRepository repository,
        ILogger<EmailConfigController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前邮件配置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<EmailConfig>> GetConfig()
    {
        try
        {
            var config = await _repository.GetConfigAsync();
            // 隐藏密码
            config.Password = string.IsNullOrEmpty(config.Password) ? "" : "********";
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取邮件配置失败");
            return StatusCode(500, new { error = "获取邮件配置失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 更新邮件配置
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EmailConfig>> UpdateConfig([FromBody] EmailConfig config)
    {
        try
        {
            // 如果密码是掩码，保留原密码
            if (config.Password == "********")
            {
                var existingConfig = await _repository.GetConfigAsync();
                config.Password = existingConfig.Password;
            }

            var savedConfig = await _repository.SaveConfigAsync(config);

            // 重新加载EmailSettings以应用新配置
            // 注意：需要重启应用或使用IOptionsMonitor才能实时生效
            _logger.LogInformation("邮件配置已更新，建议重启应用以应用新配置");

            // 隐藏密码返回
            savedConfig.Password = "********";
            return Ok(savedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新邮件配置失败");
            return StatusCode(500, new { error = "更新邮件配置失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 测试邮件配置
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult> TestEmailConfig([FromBody] TestEmailRequest request)
    {
        try
        {
            var config = await _repository.GetConfigAsync();

            if (!config.Enabled)
            {
                return BadRequest(new { error = "邮件通知未启用" });
            }

            if (string.IsNullOrEmpty(config.SmtpServer) || string.IsNullOrEmpty(config.FromEmail))
            {
                return BadRequest(new { error = "邮件配置不完整" });
            }

            // 创建临时EmailSettings和EmailService进行测试
            var emailSettings = new EmailSettings
            {
                Enabled = config.Enabled,
                SmtpServer = config.SmtpServer,
                SmtpPort = config.SmtpPort,
                UseSsl = config.UseSsl,
                FromEmail = config.FromEmail,
                FromName = config.FromName,
                Username = config.Username,
                Password = config.Password,
                ToEmails = string.IsNullOrEmpty(request.TestEmail)
                    ? config.ToEmails
                    : new List<string> { request.TestEmail }
            };

            var emailService = new EmailService(
                emailSettings,
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<EmailService>()
            );

            var success = await emailService.SendEmailAsync(
                "邮件配置测试",
                $"这是一封测试邮件，发送时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n如果您收到此邮件，说明邮件配置正确。"
            );

            if (success)
            {
                return Ok(new { message = "测试邮件已发送，请检查收件箱" });
            }
            else
            {
                return StatusCode(500, new { error = "测试邮件发送失败，请检查日志" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试邮件发送失败");
            return StatusCode(500, new { error = "测试邮件发送失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 获取常用SMTP服务器配置预设
    /// </summary>
    [HttpGet("presets")]
    public ActionResult GetSmtpPresets()
    {
        var presets = new[]
        {
            new { name = "Hotmail/Outlook", server = "smtp-mail.outlook.com", port = 587, useSsl = true },
            new { name = "Gmail", server = "smtp.gmail.com", port = 587, useSsl = true },
            new { name = "163邮箱", server = "smtp.163.com", port = 465, useSsl = true },
            new { name = "QQ邮箱", server = "smtp.qq.com", port = 587, useSsl = true },
            new { name = "Yahoo", server = "smtp.mail.yahoo.com", port = 587, useSsl = true }
        };

        return Ok(presets);
    }
}

/// <summary>
/// 测试邮件请求
/// </summary>
public class TestEmailRequest
{
    /// <summary>
    /// 测试邮件接收地址（可选，不填则使用配置中的收件人）
    /// </summary>
    public string? TestEmail { get; set; }
}
