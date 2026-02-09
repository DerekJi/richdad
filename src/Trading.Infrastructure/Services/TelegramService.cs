using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Polling;
using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Models;

namespace Trading.Infrastructure.Services;

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
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 当用户点击按钮时触发的事件
    /// </summary>
    public event EventHandler<TelegramCallbackQueryEventArgs>? OnCallbackQueryReceived;

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

    public async Task<bool> SendMessageWithButtonsAsync(
        string message,
        List<TelegramButtonRow> buttonRows,
        long? chatId = null,
        string parseMode = "Markdown")
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

            // 构建InlineKeyboard
            var keyboard = BuildInlineKeyboard(buttonRows);

            await _botClient.SendMessage(
                chatId: targetChatId.Value,
                text: message,
                parseMode: parseModeEnum,
                replyMarkup: keyboard
            );

            _logger.LogInformation("成功发送带按钮的Telegram消息到Chat ID: {ChatId}", targetChatId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送带按钮的Telegram消息失败");
            return false;
        }
    }

    public async Task<bool> AnswerCallbackQueryAsync(string callbackQueryId, string? text = null, bool showAlert = false)
    {
        try
        {
            await _botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQueryId,
                text: text,
                showAlert: showAlert
            );

            _logger.LogInformation("成功回复回调查询: {CallbackQueryId}", callbackQueryId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回复回调查询失败: {CallbackQueryId}", callbackQueryId);
            return false;
        }
    }

    public async Task<bool> EditMessageButtonsAsync(long chatId, int messageId, List<TelegramButtonRow> buttonRows)
    {
        try
        {
            var keyboard = BuildInlineKeyboard(buttonRows);

            await _botClient.EditMessageReplyMarkup(
                chatId: chatId,
                messageId: messageId,
                replyMarkup: keyboard
            );

            _logger.LogInformation("成功编辑消息按钮: Chat ID={ChatId}, Message ID={MessageId}", chatId, messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑消息按钮失败: Chat ID={ChatId}, Message ID={MessageId}", chatId, messageId);
            return false;
        }
    }

    public async Task<bool> EditMessageTextAsync(
        long chatId,
        int messageId,
        string newText,
        List<TelegramButtonRow>? buttonRows = null,
        string parseMode = "Markdown")
    {
        try
        {
            var parseModeEnum = parseMode.ToLower() switch
            {
                "markdown" => ParseMode.Markdown,
                "html" => ParseMode.Html,
                _ => ParseMode.Markdown
            };

            var keyboard = buttonRows != null ? BuildInlineKeyboard(buttonRows) : null;

            await _botClient.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: newText,
                parseMode: parseModeEnum,
                replyMarkup: keyboard
            );

            _logger.LogInformation("成功编辑消息文本: Chat ID={ChatId}, Message ID={MessageId}", chatId, messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编辑消息文本失败: Chat ID={ChatId}, Message ID={MessageId}", chatId, messageId);
            return false;
        }
    }

    public void StartReceivingUpdates()
    {
        if (_cancellationTokenSource != null)
        {
            _logger.LogWarning("Bot更新监听已在运行中");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        // 配置接收选项
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.CallbackQuery } // 只接收回调查询
        };

        // 启动长轮询
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("Telegram Bot更新监听已启动");
    }

    public void StopReceivingUpdates()
    {
        if (_cancellationTokenSource == null)
        {
            _logger.LogWarning("Bot更新监听未运行");
            return;
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;

        _logger.LogInformation("Telegram Bot更新监听已停止");
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

    /// <summary>
    /// 构建InlineKeyboard
    /// </summary>
    private InlineKeyboardMarkup BuildInlineKeyboard(List<TelegramButtonRow> buttonRows)
    {
        var keyboard = buttonRows.Select(row =>
            row.Buttons.Select(button =>
            {
                if (!string.IsNullOrEmpty(button.Url))
                {
                    return InlineKeyboardButton.WithUrl(button.Text, button.Url);
                }
                else
                {
                    return InlineKeyboardButton.WithCallbackData(button.Text, button.CallbackData);
                }
            }).ToArray()
        ).ToArray();

        return new InlineKeyboardMarkup(keyboard);
    }

    /// <summary>
    /// 处理接收到的更新
    /// </summary>
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.CallbackQuery != null)
            {
                var callbackQuery = update.CallbackQuery;
                _logger.LogInformation("收到回调查询: Data={Data}, From={User}",
                    callbackQuery.Data, callbackQuery.From.Username ?? callbackQuery.From.Id.ToString());

                // 触发事件
                var eventArgs = new TelegramCallbackQueryEventArgs
                {
                    CallbackQueryId = callbackQuery.Id,
                    CallbackData = callbackQuery.Data ?? string.Empty,
                    MessageId = callbackQuery.Message?.MessageId ?? 0,
                    ChatId = callbackQuery.Message?.Chat.Id ?? 0,
                    UserId = callbackQuery.From.Id,
                    Username = callbackQuery.From.Username,
                    MessageText = callbackQuery.Message?.Text
                };

                OnCallbackQueryReceived?.Invoke(this, eventArgs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理Telegram更新时发生错误");
        }
    }

    /// <summary>
    /// 处理轮询错误
    /// </summary>
    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram Bot轮询发生错误");
        return Task.CompletedTask;
    }
}
