using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Trading.AlertSystem.Data.Models;

namespace Trading.AlertSystem.Service.Services;

/// <summary>
/// 价格缓存服务 - 避免频繁调用数据源API
/// </summary>
public interface IPriceCacheService
{
    /// <summary>
    /// 获取缓存的价格（如果过期则返回null）
    /// </summary>
    SymbolPrice? GetCachedPrice(string symbol);

    /// <summary>
    /// 更新缓存价格
    /// </summary>
    void UpdatePrice(string symbol, SymbolPrice price);

    /// <summary>
    /// 获取所有缓存的价格
    /// </summary>
    IReadOnlyDictionary<string, SymbolPrice> GetAllCachedPrices();

    /// <summary>
    /// 缓存是否有效（未过期）
    /// </summary>
    bool IsCacheValid(string symbol);
}

public class PriceCacheService : IPriceCacheService
{
    private readonly ConcurrentDictionary<string, CachedPrice> _cache = new();
    private readonly ILogger<PriceCacheService> _logger;

    /// <summary>
    /// 缓存有效期（秒）- 默认60秒
    /// </summary>
    private const int CacheExpirationSeconds = 60;

    public PriceCacheService(ILogger<PriceCacheService> logger)
    {
        _logger = logger;
    }

    public SymbolPrice? GetCachedPrice(string symbol)
    {
        if (_cache.TryGetValue(symbol, out var cached))
        {
            if (!IsExpired(cached))
            {
                return cached.Price;
            }
        }
        return null;
    }

    public void UpdatePrice(string symbol, SymbolPrice price)
    {
        var cached = new CachedPrice
        {
            Price = price,
            CachedAt = DateTime.UtcNow
        };
        _cache.AddOrUpdate(symbol, cached, (_, _) => cached);
    }

    public IReadOnlyDictionary<string, SymbolPrice> GetAllCachedPrices()
    {
        var result = new Dictionary<string, SymbolPrice>();
        foreach (var kvp in _cache)
        {
            if (!IsExpired(kvp.Value))
            {
                result[kvp.Key] = kvp.Value.Price;
            }
        }
        return result;
    }

    public bool IsCacheValid(string symbol)
    {
        if (_cache.TryGetValue(symbol, out var cached))
        {
            return !IsExpired(cached);
        }
        return false;
    }

    private static bool IsExpired(CachedPrice cached)
    {
        return (DateTime.UtcNow - cached.CachedAt).TotalSeconds > CacheExpirationSeconds;
    }

    private class CachedPrice
    {
        public SymbolPrice Price { get; set; } = null!;
        public DateTime CachedAt { get; set; }
    }
}
