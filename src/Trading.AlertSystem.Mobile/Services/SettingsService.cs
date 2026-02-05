namespace Trading.AlertSystem.Mobile.Services;

public interface ISettingsService
{
    string ServerUrl { get; set; }
    bool AutoRefresh { get; set; }
    int RefreshIntervalSeconds { get; set; }
    void Save();
    void Load();
}

public class SettingsService : ISettingsService
{
    private const string ServerUrlKey = "server_url";
    private const string AutoRefreshKey = "auto_refresh";
    private const string RefreshIntervalKey = "refresh_interval";

    // 默认值 - 本地开发服务器
    private const string DefaultServerUrl = "http://10.0.2.2:5000"; // Android 模拟器访问本机
    private const bool DefaultAutoRefresh = true;
    private const int DefaultRefreshInterval = 30;

    public string ServerUrl { get; set; } = DefaultServerUrl;
    public bool AutoRefresh { get; set; } = DefaultAutoRefresh;
    public int RefreshIntervalSeconds { get; set; } = DefaultRefreshInterval;

    public SettingsService()
    {
        Load();
    }

    public void Save()
    {
        Preferences.Set(ServerUrlKey, ServerUrl);
        Preferences.Set(AutoRefreshKey, AutoRefresh);
        Preferences.Set(RefreshIntervalKey, RefreshIntervalSeconds);
    }

    public void Load()
    {
        ServerUrl = Preferences.Get(ServerUrlKey, DefaultServerUrl);
        AutoRefresh = Preferences.Get(AutoRefreshKey, DefaultAutoRefresh);
        RefreshIntervalSeconds = Preferences.Get(RefreshIntervalKey, DefaultRefreshInterval);
    }
}
