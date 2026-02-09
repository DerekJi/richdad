using Trading.Backtest.Data.Models;
using Microsoft.Extensions.Options;
using Trading.Backtest.Services;
using Trading.Backtest.Data.Configuration;
using Trading.Backtest.Data.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 配置AppSettings和CosmosDbSettings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

builder.Services.Configure<CosmosDbSettings>(builder.Configuration.GetSection("CosmosDb"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<CosmosDbSettings>>().Value);

// 注册数据库服务
builder.Services.AddSingleton<CosmosDbContext>();

// 注册回测服务
builder.Services.AddScoped<BacktestRunner>();

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 配置CORS（允许前端访问）
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 启用CORS
app.UseCors();

// 配置静态文件（服务HTML页面）
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
