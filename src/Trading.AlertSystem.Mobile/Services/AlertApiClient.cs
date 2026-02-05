using System.Net.Http.Json;
using System.Text.Json;
using Trading.AlertSystem.Mobile.Models;

namespace Trading.AlertSystem.Mobile.Services;

public interface IAlertApiClient
{
    // 监控状态
    Task<List<MonitorStatusItem>> GetMonitorStatusAsync();

    // 价格监控规则
    Task<List<PriceMonitorRule>> GetAllRulesAsync();
    Task<PriceMonitorRule?> GetRuleByIdAsync(string id);
    Task<PriceMonitorRule?> CreateRuleAsync(CreateRuleRequest request);
    Task<PriceMonitorRule?> UpdateRuleAsync(string id, UpdateRuleRequest request);
    Task<bool> DeleteRuleAsync(string id);
    Task<bool> ResetRuleTriggerAsync(string id);

    // 告警历史
    Task<AlertHistoryResponse> GetAlertHistoryAsync(int page = 1, int pageSize = 50);
    Task<List<AlertHistory>> GetRecentHistoryAsync(int count = 100);

    // EMA 配置
    Task<EmaMonitoringConfig?> GetEmaConfigAsync();
    Task<EmaMonitoringConfig?> UpdateEmaConfigAsync(EmaMonitoringConfig config);

    // 系统
    Task<bool> TestConnectionAsync();
}

public class AlertApiClient : IAlertApiClient
{
    private readonly ISettingsService _settings;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public AlertApiClient(ISettingsService settings)
    {
        _settings = settings;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private string BaseUrl => _settings.ServerUrl.TrimEnd('/');

    #region 监控状态

    public async Task<List<MonitorStatusItem>> GetMonitorStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/MonitorStatus");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<List<MonitorStatusItem>>(_jsonOptions);
            return result ?? new List<MonitorStatusItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetMonitorStatusAsync error: {ex.Message}");
            return new List<MonitorStatusItem>();
        }
    }

    #endregion

    #region 价格监控规则

    public async Task<List<PriceMonitorRule>> GetAllRulesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/PriceMonitor");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<List<PriceMonitorRule>>(_jsonOptions);
            return result ?? new List<PriceMonitorRule>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAllRulesAsync error: {ex.Message}");
            return new List<PriceMonitorRule>();
        }
    }

    public async Task<PriceMonitorRule?> GetRuleByIdAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/PriceMonitor/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PriceMonitorRule>(_jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetRuleByIdAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<PriceMonitorRule?> CreateRuleAsync(CreateRuleRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/PriceMonitor", request, _jsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PriceMonitorRule>(_jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreateRuleAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<PriceMonitorRule?> UpdateRuleAsync(string id, UpdateRuleRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/api/PriceMonitor/{id}", request, _jsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PriceMonitorRule>(_jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateRuleAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteRuleAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/api/PriceMonitor/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteRuleAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ResetRuleTriggerAsync(string id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{BaseUrl}/api/PriceMonitor/{id}/reset", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ResetRuleTriggerAsync error: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region 告警历史

    public async Task<AlertHistoryResponse> GetAlertHistoryAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/AlertHistory?pageNumber={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AlertHistoryResponse>(_jsonOptions);
            return result ?? new AlertHistoryResponse();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAlertHistoryAsync error: {ex.Message}");
            return new AlertHistoryResponse();
        }
    }

    public async Task<List<AlertHistory>> GetRecentHistoryAsync(int count = 100)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/AlertHistory/recent?count={count}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<List<AlertHistory>>(_jsonOptions);
            return result ?? new List<AlertHistory>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetRecentHistoryAsync error: {ex.Message}");
            return new List<AlertHistory>();
        }
    }

    #endregion

    #region EMA 配置

    public async Task<EmaMonitoringConfig?> GetEmaConfigAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/EmaMonitor");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EmaMonitoringConfig>(_jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetEmaConfigAsync error: {ex.Message}");
            return null;
        }
    }

    public async Task<EmaMonitoringConfig?> UpdateEmaConfigAsync(EmaMonitoringConfig config)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/api/EmaMonitor", config, _jsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EmaMonitoringConfig>(_jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateEmaConfigAsync error: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region 系统

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api/System/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
