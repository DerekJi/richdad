namespace Trading.Infrastructure.Services;

/// <summary>
/// 邮件通知服务接口
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// 发送纯文本邮件
    /// </summary>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件正文</param>
    /// <param name="toEmails">收件人列表（可选，默认使用配置中的收件人）</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendEmailAsync(string subject, string body, List<string>? toEmails = null);

    /// <summary>
    /// 发送HTML格式邮件
    /// </summary>
    /// <param name="subject">邮件主题</param>
    /// <param name="htmlBody">HTML正文</param>
    /// <param name="toEmails">收件人列表（可选）</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendHtmlEmailAsync(string subject, string htmlBody, List<string>? toEmails = null);

    /// <summary>
    /// 发送带附件的邮件
    /// </summary>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件正文</param>
    /// <param name="attachmentStream">附件流</param>
    /// <param name="attachmentName">附件名称</param>
    /// <param name="toEmails">收件人列表（可选）</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendEmailWithAttachmentAsync(string subject, string body, Stream attachmentStream, string attachmentName, List<string>? toEmails = null);
}
