using Trading.Infrastructure.Configuration;
using Trading.Infrastructure.Services;
using Trading.Web.Services;

namespace Trading.Web.Configuration;

/// <summary>
/// 通知服务配置扩展方法
/// </summary>
public static class NotificationConfiguration
{
    /// <summary>
    /// 注册通知服务（Email, Telegram）
    /// </summary>
    public static IServiceCollection AddNotificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册邮件服务
        if (services.Any(x => x.ServiceType == typeof(EmailSettings)))
        {
            services.AddSingleton<IEmailService, EmailService>();
        }
        else
        {
            // 如果没有 CosmosDB，从 appsettings 加载
            var emailConfig = configuration.GetSection("Email");
            if (!string.IsNullOrEmpty(emailConfig["SmtpServer"]) && !string.IsNullOrEmpty(emailConfig["FromEmail"]))
            {
                services.AddSingleton<IEmailService, EmailService>();
            }
        }

        // 注册 Telegram 服务
        var telegramConfig = configuration.GetSection("Telegram");
        if (!string.IsNullOrEmpty(telegramConfig["BotToken"]))
        {
            services.AddSingleton<ITelegramService, TelegramService>();
        }
        else
        {
            services.AddSingleton<ITelegramService, DemoTelegramService>();
        }

        return services;
    }
}
