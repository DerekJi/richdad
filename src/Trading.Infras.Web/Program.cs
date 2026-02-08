using Trading.AlertSystem.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 配置优先级（从低到高）：
// 1. appsettings.json (已由 WebApplication.CreateBuilder 自动加载)
// 2. appsettings.{Environment}.json (已由 WebApplication.CreateBuilder 自动加载)
// 3. 环境变量
// 4. User Secrets (开发环境，优先级最高)
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddUserSecrets<Program>(optional: true);

// 注册所有服务
builder.Services.AddApplicationSettings(builder.Configuration);

// 数据存储层 - 优先使用 Azure Table Storage，如果未配置则使用内存存储
// Cosmos DB 已禁用以降低成本
builder.Services.AddAzureTableStorageServices(builder.Configuration);
builder.Services.AddStorageFallback(builder.Configuration);

builder.Services.AddDataSourceServices(builder.Configuration);
builder.Services.AddNotificationServices(builder.Configuration);
builder.Services.AddAIServices(builder.Configuration);  // 添加AI服务（可选）
builder.Services.AddBusinessServices();
builder.Services.AddBackgroundServices();
builder.Services.AddWebServices();

var app = builder.Build();

// 初始化数据存储
await app.InitializeStorageAsync();

// 配置 Web 应用程序
app.ConfigureWebApplication();

app.Run();
