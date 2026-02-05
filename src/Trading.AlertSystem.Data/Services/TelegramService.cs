using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Trading.AlertSystem.Data.Configuration;

namespace Trading.AlertSystem.Data.Services;

/// <summary>
/// Telegram Bot服务实现
/// </summary>
public class TelegramService : ITelegramService
{
    private readonly TelegramBotClient _botClient;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramService> _logger;
    private readonly IEmailService? _emailService;
    private readonly EmailSettings? _emailSettings;

    public TelegramService(
        TelegramSettings settings,
        ILogger<TelegramService> logger,
        IEmailService? emailService = null,
        EmailSettings? emailSettings = null)
    {
        _settings = settings;
        _logger = logger;
        _emailService = emailService;
        _emailSettings = emailSettings;
        _botClient = new TelegramBotClient(_settings.BotToken);
    }

    public async Task<bool> SendMessageAsync(string message, long? chatId = null)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Telegram通知已禁用，尝试邮件通知");
            return await SendEmailFallbackAsync("Telegram已禁用", message);
        }

        try
        {
            var targetChatId = chatId ?? _settings.DefaultChatId;
            if (targetChatId == null)
            {
                _logger.LogError("未指定Telegram Chat ID，尝试邮件通知");
                return await SendEmailFallbackAsync("Telegram配置错误", message);
            }

            await _botClient.SendMessage(
                chatId: targetChatId.Value,
                text: message
            );

            _logger.LogInformation("成功发送Telegram消息到Chat ID: {ChatId}", targetChatId);

            // 同时发送邮件（如果配置为始终发送）
            if (_emailService != null && _emailSettings != null &&
                _emailSettings.Enabled && !_emailSettings.OnlyOnTelegramFailure)
            {
                await _emailService.SendEmailAsync("交易提醒", message);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送Telegram消息失败，尝试邮件通知");
            return await SendEmailFallbackAsync("Telegram发送失败", message);
        }
    }

    public async Task<bool> SendFormattedMessageAsync(string message, long? chatId = null, string parseMode = "Markdown")
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Telegram通知已禁用，跳过发送消息");
            return false;
        }

        try
        {
            var targetChatId = chatId ?? _settings.DefaultChatId;
            if (targetChatId == null)
            {
                _logger.LogError("未指定Telegram Chat ID");
                return false;
            }

            var parseModeEnum = parseMode.ToLower() switch
            {
                "markdown" => ParseMode.Markdown,
                "html" => ParseMode.Html,
                _ => ParseMode.Markdown
            };

            await _botClient.SendMessage(
                chatId: targetChatId.Value,
                text: message,
                parseMode: parseModeEnum
            );

            _logger.LogInformation("成功发送格式化Telegram消息到Chat ID: {ChatId}", targetChatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送格式化Telegram消息失败");
            return false;
        }
    }

    public async Task<bool> SendPhotoAsync(Stream photoStream, string? caption = null, long? chatId = null)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Telegram通知已禁用，尝试邮件通知");
            return await SendEmailWithImageFallbackAsync("Telegram已禁用", caption ?? "交易图表", photoStream);
        }

        try
        {
            var targetChatId = chatId ?? _settings.DefaultChatId;
            if (targetChatId == null)
            {
                _logger.LogError("未指定Telegram Chat ID，尝试邮件通知");
                return await SendEmailWithImageFallbackAsync("Telegram配置错误", caption ?? "交易图表", photoStream);
            }

            photoStream.Position = 0; // 重置流位置
            var inputFile = InputFile.FromStream(photoStream, "chart.png");

            await _botClient.SendPhoto(
                chatId: targetChatId.Value,
                photo: inputFile,
                caption: caption
            );

            _logger.LogInformation("成功发送Telegram图片到Chat ID: {ChatId}", targetChatId);

            // 同时发送邮件（如果配置为始终发送）
            if (_emailService != null && _emailSettings != null &&
                _emailSettings.Enabled && !_emailSettings.OnlyOnTelegramFailure)
            {
                photoStream.Position = 0;
                await SendEmailWithImageFallbackAsync("交易图表提醒", caption ?? "交易图表", photoStream);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送Telegram图片失败，尝试邮件通知");
            return await SendEmailWithImageFallbackAsync("Telegram发送失败", caption ?? "交易图表", photoStream);
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var me = await _botClient.GetMe();
            _logger.LogInformation("成功连接到Telegram Bot: @{Username}", me.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试Telegram连接失败");
            return false;
        }
    }

    /// <summary>
    /// 发送邮件作为备用通知方式
    /// </summary>
    private async Task<bool> SendEmailFallbackAsync(string subject, string body)
    {
        if (_emailService == null || _emailSettings == null || !_emailSettings.Enabled)
        {
            return false;
        }

        try
        {
            _logger.LogInformation("尝试通过邮件发送通知：{Subject}", subject);
            return await _emailService.SendEmailAsync($"[Trading Alert] {subject}", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邮件备用通知也失败了");
            return false;
        }
    }

    /// <summary>
    /// 发送带图片附件的邮件作为备用通知方式
    /// </summary>
    private async Task<bool> SendEmailWithImageFallbackAsync(string subject, string body, Stream imageStream)
    {
        if (_emailService == null || _emailSettings == null || !_emailSettings.Enabled)
        {
            return false;
        }

        try
        {
            _logger.LogInformation("尝试通过邮件发送带图片的通知：{Subject}", subject);
            imageStream.Position = 0;
            return await _emailService.SendEmailWithAttachmentAsync(
                $"[Trading Alert] {subject}",
                body,
                imageStream,
                "chart.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邮件备用通知也失败了");
            return false;
        }
    }
}
