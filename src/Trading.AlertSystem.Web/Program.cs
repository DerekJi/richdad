using Microsoft.Extensions.Options;
using Trading.AlertSystem.Data.Configuration;
using Trading.AlertSystem.Data.Services;
using Trading.AlertSystem.Service.Configuration;
using Trading.AlertSystem.Service.Repositories;
using Trading.AlertSystem.Service.Services;
using Trading.Data.Configuration;
using Trading.Data.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 配置设置
builder.Services.Configure<TradeLockerSettings>(builder.Configuration.GetSection("TradeLocker"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<TradeLockerSettings>>().Value);

builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("Telegram"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<TelegramSettings>>().Value);

builder.Services.Configure<MonitoringSettings>(builder.Configuration.GetSection("Monitoring"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MonitoringSettings>>().Value);

builder.Services.Configure<CosmosDbSettings>(builder.Configuration.GetSection("CosmosDb"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<CosmosDbSettings>>().Value);

// 注册数据库服务
builder.Services.AddSingleton<CosmosDbContext>();

// 注册数据层服务
builder.Services.AddHttpClient<ITradeLockerService, TradeLockerService>();
builder.Services.AddSingleton<ITelegramService, TelegramService>();

// 注册仓储
builder.Services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();

// 注册业务服务
builder.Services.AddSingleton<IPriceMonitorService, PriceMonitorService>();

// 添加后台服务（自动启动价格监控）
builder.Services.AddHostedService<PriceMonitorHostedService>();

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

/// <summary>
/// 价格监控后台服务
/// </summary>
public class PriceMonitorHostedService : IHostedService
{
    private readonly IPriceMonitorService _monitorService;
    private readonly ILogger<PriceMonitorHostedService> _logger;

    public PriceMonitorHostedService(
        IPriceMonitorService monitorService,
        ILogger<PriceMonitorHostedService> logger)
    {
        _monitorService = monitorService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动价格监控后台服务");
        await _monitorService.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("停止价格监控后台服务");
        await _monitorService.StopAsync();
    }
}
