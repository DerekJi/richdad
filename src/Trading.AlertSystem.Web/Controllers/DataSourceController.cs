using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Trading.AlertSystem.Data.Configuration;
using Trading.AlertSystem.Data.Services;

namespace Trading.AlertSystem.Web.Controllers;

/// <summary>
/// 数据源配置API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataSourceController : ControllerBase
{
    private readonly IOptionsMonitor<DataSourceSettings> _settingsMonitor;
    private readonly IMarketDataService _marketDataService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataSourceController> _logger;

    public DataSourceController(
        IOptionsMonitor<DataSourceSettings> settingsMonitor,
        IMarketDataService marketDataService,
        IConfiguration configuration,
        ILogger<DataSourceController> logger)
    {
        _settingsMonitor = settingsMonitor;
        _marketDataService = marketDataService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前数据源配置
    /// </summary>
    [HttpGet]
    public ActionResult GetDataSource()
    {
        var settings = _settingsMonitor.CurrentValue;
        var currentProvider = _marketDataService.GetCurrentProvider();

        return Ok(new
        {
            provider = currentProvider,
            availableProviders = new[] { "TradeLocker", "Oanda" }
        });
    }

    /// <summary>
    /// 设置数据源
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> SetDataSource([FromBody] SetDataSourceRequest request)
    {
        if (string.IsNullOrEmpty(request.Provider))
        {
            return BadRequest(new { message = "Provider不能为空" });
        }

        var validProviders = new[] { "TradeLocker", "Oanda" };
        if (!validProviders.Contains(request.Provider, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = $"无效的Provider，必须是：{string.Join(", ", validProviders)}" });
        }

        try
        {
            // 更新appsettings.json文件
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = await System.IO.File.ReadAllTextAsync(appSettingsPath);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            using var stream = new MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name == "DataSource")
                    {
                        writer.WritePropertyName("DataSource");
                        writer.WriteStartObject();
                        writer.WriteString("Provider", request.Provider);
                        writer.WriteEndObject();
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            await System.IO.File.WriteAllTextAsync(appSettingsPath, updatedJson);

            _logger.LogInformation("数据源已切换到: {Provider}", request.Provider);

            return Ok(new
            {
                success = true,
                message = $"数据源已切换到 {request.Provider}",
                note = "配置已保存，但需要重启服务才能生效"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换数据源时发生错误");
            return StatusCode(500, new { message = $"切换数据源失败: {ex.Message}" });
        }
    }
}

public class SetDataSourceRequest
{
    public string Provider { get; set; } = string.Empty;
}
