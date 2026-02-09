using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Trading.Infrastructure.Configuration;

namespace Trading.Infrastructure.Services;

/// <summary>
/// 邮件通知服务实现
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(EmailSettings settings, ILogger<EmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string subject, string body, List<string>? toEmails = null)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("邮件通知已禁用，跳过发送");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpServer))
        {
            _logger.LogError("SMTP服务器地址未配置");
            return false;
        }

        var recipients = toEmails ?? _settings.ToEmails;
        if (recipients.Count == 0)
        {
            _logger.LogError("未配置收件人邮箱");
            return false;
        }

        try
        {
            _logger.LogDebug("正在连接SMTP服务器: {SmtpServer}:{SmtpPort}", _settings.SmtpServer, _settings.SmtpPort);
            using var smtpClient = CreateSmtpClient();
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("成功发送邮件到 {Recipients}", string.Join(", ", recipients));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送邮件失败，SMTP服务器: {SmtpServer}:{SmtpPort}", _settings.SmtpServer, _settings.SmtpPort);
            return false;
        }
    }

    public async Task<bool> SendHtmlEmailAsync(string subject, string htmlBody, List<string>? toEmails = null)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("邮件通知已禁用，跳过发送");
            return false;
        }

        var recipients = toEmails ?? _settings.ToEmails;
        if (recipients.Count == 0)
        {
            _logger.LogError("未配置收件人邮箱");
            return false;
        }

        try
        {
            using var smtpClient = CreateSmtpClient();
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("成功发送HTML邮件到 {Recipients}", string.Join(", ", recipients));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送HTML邮件失败");
            return false;
        }
    }

    public async Task<bool> SendEmailWithAttachmentAsync(string subject, string body, Stream attachmentStream, string attachmentName, List<string>? toEmails = null)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("邮件通知已禁用，跳过发送");
            return false;
        }

        var recipients = toEmails ?? _settings.ToEmails;
        if (recipients.Count == 0)
        {
            _logger.LogError("未配置收件人邮箱");
            return false;
        }

        try
        {
            using var smtpClient = CreateSmtpClient();
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            // 添加附件
            attachmentStream.Position = 0;
            var attachment = new Attachment(attachmentStream, attachmentName);
            mailMessage.Attachments.Add(attachment);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("成功发送带附件的邮件到 {Recipients}", string.Join(", ", recipients));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送带附件的邮件失败");
            return false;
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        return new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = _settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
    }
}
