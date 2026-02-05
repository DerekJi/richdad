using Microsoft.Extensions.Options;
using Trading.AlertSystem.Data.Configuration;
using Trading.AlertSystem.Data.Services;
using Trading.AlertSystem.Service.Configuration;
using Trading.AlertSystem.Service.Repositories;
using Trading.AlertSystem.Service.Services;
using Trading.AlertSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加 User Secrets 支持（开发和测试环境）
builder.Configuration.AddUserSecrets<Program>(optional: true);

// 配置设置
builder.Services.Configure<DataSourceSettings>(builder.Configuration.GetSection("DataSource"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<DataSourceSettings>>().Value);

builder.Services.Configure<TradeLockerSettings>(builder.Configuration.GetSection("TradeLocker"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<TradeLockerSettings>>().Value);

builder.Services.Configure<OandaSettings>(builder.Configuration.GetSection("Oanda"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OandaSettings>>().Value);

builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("Telegram"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<TelegramSettings>>().Value);

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmailSettings>>().Value);

builder.Services.Configure<MonitoringSettings>(builder.Configuration.GetSection("Monitoring"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MonitoringSettings>>().Value);

builder.Services.Configure<EmaMonitoringSettings>(builder.Configuration.GetSection("EmaMonitoring"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmaMonitoringSettings>>().Value);

// 注册数据库服务（如果配置了CosmosDB）
var cosmosConfig = builder.Configuration.GetSection("CosmosDb");
var connectionString = cosmosConfig["ConnectionString"];

if (!string.IsNullOrEmpty(connectionString))
{
    var cosmosSettings = new Trading.AlertSystem.Data.Configuration.CosmosDbSettings
    {
        ConnectionString = connectionString,
        DatabaseName = cosmosConfig["DatabaseName"] ?? "TradingSystem",
        AlertContainerName = cosmosConfig["AlertContainerName"] ?? "PriceAlerts",
        AlertHistoryContainerName = cosmosConfig["AlertHistoryContainerName"] ?? "AlertHistory",
        EmaConfigContainerName = cosmosConfig["EmaConfigContainerName"] ?? "EmaConfig",
        DataSourceConfigContainerName = cosmosConfig["DataSourceConfigContainerName"] ?? "DataSourceConfig",
        EmailConfigContainerName = cosmosConfig["EmailConfigContainerName"] ?? "EmailConfig"
    };

    builder.Services.AddSingleton(cosmosSettings);
    builder.Services.AddSingleton<Trading.AlertSystem.Data.Infrastructure.CosmosDbContext>();
    builder.Services.AddSingleton<IPriceAlertRepository, PriceAlertRepository>();
    builder.Services.AddSingleton<Trading.AlertSystem.Data.Repositories.IAlertHistoryRepository, Trading.AlertSystem.Data.Repositories.AlertHistoryRepository>();
    builder.Services.AddSingleton<Trading.AlertSystem.Data.Repositories.IEmaConfigRepository, Trading.AlertSystem.Data.Repositories.EmaConfigRepository>();
    builder.Services.AddSingleton<Trading.AlertSystem.Data.Repositories.IDataSourceConfigRepository, Trading.AlertSystem.Data.Repositories.DataSourceConfigRepository>();
    builder.Services.AddSingleton<Trading.AlertSystem.Data.Repositories.IEmailConfigRepository, Trading.AlertSystem.Data.Repositories.EmailConfigRepository>();

    // 注册一个延迟初始化的DataSourceSettings
    // 它会在第一次使用时从数据库加载
    builder.Services.AddSingleton<DataSourceSettings>(serviceProvider =>
    {
        // 同步包装异步操作（在单例工厂中）
        var dbContext = serviceProvider.GetRequiredService<Trading.AlertSystem.Data.Infrastructure.CosmosDbContext>();
        var dataSourceRepo = serviceProvider.GetRequiredService<Trading.AlertSystem.Data.Repositories.IDataSourceConfigRepository>();

        // 初始化数据库并加载配置
        dbContext.InitializeAsync().GetAwaiter().GetResult();
        var dataSourceConfig = dataSourceRepo.GetConfigAsync().GetAwaiter().GetResult();

        return new DataSourceSettings { Provider = dataSourceConfig.Provider };
    });

    // 从数据库加载邮件配置
    builder.Services.AddSingleton<EmailSettings>(serviceProvider =>
    {
        var dbContext = serviceProvider.GetRequiredService<Trading.AlertSystem.Data.Infrastructure.CosmosDbContext>();
        var emailRepo = serviceProvider.GetRequiredService<Trading.AlertSystem.Data.Repositories.IEmailConfigRepository>();

        dbContext.InitializeAsync().GetAwaiter().GetResult();
        var emailConfig = emailRepo.GetConfigAsync().GetAwaiter().GetResult();

        return new EmailSettings
        {
            Enabled = emailConfig.Enabled,
            SmtpServer = emailConfig.SmtpServer,
            SmtpPort = emailConfig.SmtpPort,
            UseSsl = emailConfig.UseSsl,
            FromEmail = emailConfig.FromEmail,
            FromName = emailConfig.FromName,
            Username = emailConfig.Username,
            Password = emailConfig.Password,
            ToEmails = emailConfig.ToEmails,
            OnlyOnTelegramFailure = emailConfig.OnlyOnTelegramFailure
        };
    });
}
else
{
    // 使用内存存储作为后备方案
    builder.Services.AddSingleton<IPriceAlertRepository, InMemoryPriceAlertRepository>();
    // AlertHistoryRepository 需要 CosmosDB，不提供内存版本

    // 如果没有CosmosDB，使用默认配置
    builder.Services.AddSingleton(new DataSourceSettings { Provider = "Oanda" });
}

// 注册数据层服务
var tradeLockerConfig = builder.Configuration.GetSection("TradeLocker");
if (!string.IsNullOrEmpty(tradeLockerConfig["Email"]) && !string.IsNullOrEmpty(tradeLockerConfig["Password"]))
{
    builder.Services.AddHttpClient<ITradeLockerService, TradeLockerService>();
}
else
{
    builder.Services.AddSingleton<ITradeLockerService, DemoTradeLockerService>();
}

// 注册OANDA服务
var oandaConfig = builder.Configuration.GetSection("Oanda");
if (!string.IsNullOrEmpty(oandaConfig["ApiKey"]) && !string.IsNullOrEmpty(oandaConfig["AccountId"]))
{
    builder.Services.AddHttpClient<IOandaService, OandaService>();
}

// 注册统一的市场数据服务（根据配置自动路由）
// 注册统一的市场数据服务（根据配置自动路由）
// 注册统一的市场数据服务（根据配置自动路由）
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();

// 注册邮件服务（如果有EmailSettings配置）
if (builder.Services.Any(x => x.ServiceType == typeof(EmailSettings)))
{
    builder.Services.AddSingleton<IEmailService, EmailService>();
}
else
{
    // 如果没有CosmosDB，从appsettings加载
    var emailConfig = builder.Configuration.GetSection("Email");
    if (!string.IsNullOrEmpty(emailConfig["SmtpServer"]) && !string.IsNullOrEmpty(emailConfig["FromEmail"]))
    {
        builder.Services.AddSingleton<IEmailService, EmailService>();
    }
}

var telegramConfig = builder.Configuration.GetSection("Telegram");
if (!string.IsNullOrEmpty(telegramConfig["BotToken"]))
{
    builder.Services.AddSingleton<ITelegramService, TelegramService>();
}
else
{
    builder.Services.AddSingleton<ITelegramService, DemoTelegramService>();
}

// 注册业务服务
builder.Services.AddSingleton<IPriceMonitorService, PriceMonitorService>();
builder.Services.AddSingleton<IChartService, ChartService>();
builder.Services.AddSingleton<IEmaMonitoringService, EmaMonitoringService>();

// 添加后台服务（自动启动价格监控）
builder.Services.AddHostedService<PriceMonitorHostedService>();

// 添加EMA监测后台服务
builder.Services.AddHostedService<EmaMonitoringHostedService>();

// 添加TradeLocker启动测试服务
// builder.Services.AddHostedService<TradeLockerStartupTestService>();

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Trading Alert System API", Version = "v1" });
});

var app = builder.Build();

// 初始化 Cosmos DB
var cosmosDbContext = app.Services.GetService<Trading.AlertSystem.Data.Infrastructure.CosmosDbContext>();
if (cosmosDbContext != null)
{
    await cosmosDbContext.InitializeAsync();

    // 使用 appsettings 中的值初始化 EMA 配置（如果数据库中不存在）
    var emaConfigRepo = app.Services.GetService<Trading.AlertSystem.Data.Repositories.IEmaConfigRepository>();
    var emaSettings = app.Services.GetService<EmaMonitoringSettings>();
    if (emaConfigRepo != null && emaSettings != null)
    {
        await emaConfigRepo.InitializeDefaultConfigAsync(
            emaSettings.Enabled,
            emaSettings.Symbols,
            emaSettings.TimeFrames,
            emaSettings.EmaPeriods,
            emaSettings.HistoryMultiplier);
    }

    // 初始化邮件配置（如果数据库中不存在）
    var emailConfigRepo = app.Services.GetService<Trading.AlertSystem.Data.Repositories.IEmailConfigRepository>();
    if (emailConfigRepo != null)
    {
        await emailConfigRepo.InitializeDefaultConfigAsync();
    }
}

// 启用CORS
app.UseCors();

// 启用Swagger（开发和生产环境）
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading Alert System API V1");
    c.RoutePrefix = "swagger";
});

// 配置静态文件
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
