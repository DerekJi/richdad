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
builder.Services.Configure<TradeLockerSettings>(builder.Configuration.GetSection("TradeLocker"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<TradeLockerSettings>>().Value);

builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("Telegram"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<TelegramSettings>>().Value);

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
        AlertHistoryContainerName = cosmosConfig["AlertHistoryContainerName"] ?? "AlertHistory"
    };

    builder.Services.AddSingleton(cosmosSettings);
    builder.Services.AddSingleton<Trading.AlertSystem.Data.Infrastructure.CosmosDbContext>();
    builder.Services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();
    builder.Services.AddScoped<Trading.AlertSystem.Data.Repositories.IAlertHistoryRepository, Trading.AlertSystem.Data.Repositories.AlertHistoryRepository>();
}
else
{
    // 使用内存存储作为后备方案
    builder.Services.AddSingleton<IPriceAlertRepository, InMemoryPriceAlertRepository>();
    // AlertHistoryRepository 需要 CosmosDB，不提供内存版本
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
