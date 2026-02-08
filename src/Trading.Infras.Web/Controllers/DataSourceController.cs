using Microsoft.AspNetCore.Mvc;
using Trading.Infras.Data.Models;
using Trading.Infras.Data.Repositories;

namespace Trading.Infras.Web.Controllers;

/// <summary>
/// 数据源配置API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DataSourceController : ControllerBase
{
    private readonly IDataSourceConfigRepository _configRepository;
    private readonly ILogger<DataSourceController> _logger;

    public DataSourceController(
        IDataSourceConfigRepository configRepository,
        ILogger<DataSourceController> logger)
    {
        _configRepository = configRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前数据源配置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetDataSource()
    {
        try
        {
            var config = await _configRepository.GetConfigAsync();

            return Ok(new
            {
                provider = config.Provider,
                availableProviders = new[] { "TradeLocker", "Oanda" },
                lastUpdated = config.LastUpdated,
                updatedBy = config.UpdatedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据源配置失败");
            return StatusCode(500, new { message = "获取配置失败" });
        }
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
            var config = new DataSourceConfig
            {
                Provider = request.Provider,
                UpdatedBy = "User"
            };

            await _configRepository.UpdateConfigAsync(config);

            _logger.LogInformation("数据源已切换到: {Provider}", request.Provider);

            return Ok(new
            {
                success = true,
                message = $"数据源已切换到 {request.Provider}",
                note = "配置已保存到数据库，需要重启服务才能生效"
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
