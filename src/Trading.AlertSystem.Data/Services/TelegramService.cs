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

    public TelegramService(TelegramSettings settings, ILogger<TelegramService> logger)
    {
        _settings = settings;
        _logger = logger;
        _botClient = new TelegramBotClient(_settings.BotToken);
    }

    public async Task<bool> SendMessageAsync(string message, long? chatId = null)
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

            await _botClient.SendMessage(
                chatId: targetChatId.Value,
                text: message
            );

            _logger.LogInformation("成功发送Telegram消息到Chat ID: {ChatId}", targetChatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送Telegram消息失败");
            return false;
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
            _logger.LogInformation("Telegram通知已禁用，跳过发送图片");
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

            photoStream.Position = 0; // 重置流位置
            var inputFile = InputFile.FromStream(photoStream, "chart.png");

            await _botClient.SendPhoto(
                chatId: targetChatId.Value,
                photo: inputFile,
                caption: caption
            );

            _logger.LogInformation("成功发送Telegram图片到Chat ID: {ChatId}", targetChatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送Telegram图片失败");
            return false;
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
}
