using System.Collections.Concurrent;
using System.Text.Json;
using ExamSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ExamSystem.Infrastructure.Caching;

/// <summary>
/// 带自动降级与自动恢复的缓存服务。
/// - Redis 不可用时自动切换为内存缓存
/// - Redis 恢复后自动将内存中的条目同步回 Redis
/// </summary>
public sealed class ResilientCacheService : ICacheService, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ResilientCacheService> _logger;
    private readonly Timer _healthTimer;

    // volatile 保证多线程可见性
    private volatile bool _redisAvailable;

    // 内存镜像：始终与写入操作同步，作为降级存储和恢复时同步源
    private readonly ConcurrentDictionary<string, MemoryCacheEntry> _memoryStore = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Redis 健康检查间隔
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromSeconds(30);

    public ResilientCacheService(IConnectionMultiplexer redis, ILogger<ResilientCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
        _redisAvailable = redis.IsConnected;

        if (!_redisAvailable)
            _logger.LogWarning("Redis 不可用，已启用内存缓存降级模式。");

        _healthTimer = new Timer(CheckRedisHealth, null, HealthCheckInterval, HealthCheckInterval);
    }

    // ── 健康检查 & 同步 ──────────────────────────────────────────────────────

    private void CheckRedisHealth(object? state)
    {
        try
        {
            _redis.GetDatabase().Ping();

            if (!_redisAvailable)
            {
                _logger.LogInformation("Redis 连接已恢复，开始将内存缓存同步到 Redis …");
                SyncMemoryToRedis();
                _redisAvailable = true;
                _logger.LogInformation("Redis 同步完成。");
            }
        }
        catch
        {
            if (_redisAvailable)
            {
                _logger.LogWarning("Redis 连接断开，已切换为内存缓存降级模式。");
                _redisAvailable = false;
            }
        }
    }

    private void SyncMemoryToRedis()
    {
        var db = _redis.GetDatabase();
        var expiredKeys = new List<string>();

        foreach (var (key, entry) in _memoryStore)
        {
            if (entry.IsExpired)
            {
                expiredKeys.Add(key);
                continue;
            }

            try
            {
                var ttl = entry.ExpiresAt.HasValue
                    ? entry.ExpiresAt.Value - DateTime.UtcNow
                    : (TimeSpan?)null;

                db.StringSet(key, entry.JsonValue, ttl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "同步键 {Key} 到 Redis 失败。", key);
            }
        }

        // 清理内存中已过期的条目
        foreach (var key in expiredKeys)
            _memoryStore.TryRemove(key, out _);
    }

    // ── ICacheService 实现 ──────────────────────────────────────────────────

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_redisAvailable)
        {
            try
            {
                var value = await _redis.GetDatabase().StringGetAsync(key);
                if (value.HasValue)
                    return JsonSerializer.Deserialize<T>(value!, JsonOptions);

                return default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 读取失败（键：{Key}），降级到内存缓存。", key);
                _redisAvailable = false;
            }
        }

        // 内存降级
        if (_memoryStore.TryGetValue(key, out var entry) && !entry.IsExpired)
            return JsonSerializer.Deserialize<T>(entry.JsonValue, JsonOptions);

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var expiresAt = expiry.HasValue ? DateTime.UtcNow + expiry.Value : (DateTime?)null;

        // 始终镜像到内存，供降级访问和恢复时同步
        _memoryStore[key] = new MemoryCacheEntry(json, expiresAt);

        if (_redisAvailable)
        {
            try
            {
                await _redis.GetDatabase().StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(10));
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 写入失败（键：{Key}），已写入内存缓存。", key);
                _redisAvailable = false;
            }
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryStore.TryRemove(key, out _);

        if (_redisAvailable)
        {
            try
            {
                await _redis.GetDatabase().KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 删除失败（键：{Key}）。", key);
                _redisAvailable = false;
            }
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // 内存中按前缀删除
        var keysToRemove = _memoryStore.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
            _memoryStore.TryRemove(key, out _);

        if (_redisAvailable)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: $"{prefix}*").ToArray();
                if (keys.Length > 0)
                    await _redis.GetDatabase().KeyDeleteAsync(keys);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis 前缀删除失败（前缀：{Prefix}）。", prefix);
                _redisAvailable = false;
            }
        }
    }

    public void Dispose()
    {
        _healthTimer.Dispose();
    }
}

/// <summary>内存缓存条目（含过期时间）</summary>
internal sealed record MemoryCacheEntry(string JsonValue, DateTime? ExpiresAt)
{
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}
