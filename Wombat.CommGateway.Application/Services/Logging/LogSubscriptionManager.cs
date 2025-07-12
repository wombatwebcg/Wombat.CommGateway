using System.Collections.Concurrent;
using Wombat.CommGateway.Domain.Enums;
using Wombat.CommGateway.Application.Common.Logging;
using Wombat.Extensions.AutoGenerator.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevel = Wombat.CommGateway.Domain.Enums.LogLevel;

namespace Wombat.CommGateway.Application.Services.Logging;

/// <summary>
/// 日志订阅管理器实现
/// </summary>
[AutoInject<ILogSubscriptionManager>(ServiceLifetime = ServiceLifetime.Singleton)]
public class LogSubscriptionManager : ILogSubscriptionManager
{
    private readonly ILogger<LogSubscriptionManager> _logger;
    
    // 类别订阅：连接ID -> 类别集合
    private readonly ConcurrentDictionary<string, HashSet<LogCategory>> _categorySubscriptions = new();
    
    // 级别订阅：连接ID -> 级别集合
    private readonly ConcurrentDictionary<string, HashSet<LogLevel>> _levelSubscriptions = new();
    
    // 连接最后活动时间
    private readonly ConcurrentDictionary<string, DateTime> _connectionLastActivity = new();
    
    private readonly object _lock = new();

    public LogSubscriptionManager(ILogger<LogSubscriptionManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 类别订阅
    public Task SubscribeToCategoryAsync(string connectionId, LogCategory category)
    {
        if (string.IsNullOrEmpty(connectionId))
            throw new ArgumentNullException(nameof(connectionId));

        lock (_lock)
        {
            var subscriptions = _categorySubscriptions.GetOrAdd(connectionId, _ => new HashSet<LogCategory>());
            var added = subscriptions.Add(category);
            
            if (added)
            {
                UpdateLastActivity(connectionId);
                _logger.LogDebug("连接 {ConnectionId} 订阅日志类别: {Category}", connectionId, category);
            }
        }
        
        return Task.CompletedTask;
    }

    public Task UnsubscribeFromCategoryAsync(string connectionId, LogCategory category)
    {
        if (string.IsNullOrEmpty(connectionId))
            throw new ArgumentNullException(nameof(connectionId));

        lock (_lock)
        {
            if (_categorySubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                var removed = subscriptions.Remove(category);
                
                if (removed)
                {
                    UpdateLastActivity(connectionId);
                    _logger.LogDebug("连接 {ConnectionId} 取消订阅日志类别: {Category}", connectionId, category);
                }
                
                // 如果该连接的类别订阅为空，则移除整个条目
                if (subscriptions.Count == 0)
                {
                    _categorySubscriptions.TryRemove(connectionId, out _);
                }
            }
        }
        
        return Task.CompletedTask;
    }

    public IReadOnlyList<LogCategory> GetCategorySubscriptions(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            return Array.Empty<LogCategory>();

        lock (_lock)
        {
            return _categorySubscriptions.TryGetValue(connectionId, out var subscriptions) 
                ? subscriptions.ToArray() 
                : Array.Empty<LogCategory>();
        }
    }

    public IReadOnlyList<string> GetCategorySubscribers(LogCategory category)
    {
        lock (_lock)
        {
            var subscribers = new List<string>();
            
            foreach (var kvp in _categorySubscriptions)
            {
                if (kvp.Value.Contains(category))
                {
                    subscribers.Add(kvp.Key);
                }
            }
            
            return subscribers;
        }
    }
    #endregion

    #region 级别订阅
    public Task SubscribeToLevelAsync(string connectionId, LogLevel level)
    {
        if (string.IsNullOrEmpty(connectionId))
            throw new ArgumentNullException(nameof(connectionId));

        lock (_lock)
        {
            var subscriptions = _levelSubscriptions.GetOrAdd(connectionId, _ => new HashSet<LogLevel>());
            var added = subscriptions.Add(level);
            
            if (added)
            {
                UpdateLastActivity(connectionId);
                _logger.LogDebug("连接 {ConnectionId} 订阅日志级别: {Level}", connectionId, level);
            }
        }
        
        return Task.CompletedTask;
    }

    public Task UnsubscribeFromLevelAsync(string connectionId, LogLevel level)
    {
        if (string.IsNullOrEmpty(connectionId))
            throw new ArgumentNullException(nameof(connectionId));

        lock (_lock)
        {
            if (_levelSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                var removed = subscriptions.Remove(level);
                
                if (removed)
                {
                    UpdateLastActivity(connectionId);
                    _logger.LogDebug("连接 {ConnectionId} 取消订阅日志级别: {Level}", connectionId, level);
                }
                
                // 如果该连接的级别订阅为空，则移除整个条目
                if (subscriptions.Count == 0)
                {
                    _levelSubscriptions.TryRemove(connectionId, out _);
                }
            }
        }
        
        return Task.CompletedTask;
    }

    public IReadOnlyList<LogLevel> GetLevelSubscriptions(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            return Array.Empty<LogLevel>();

        lock (_lock)
        {
            return _levelSubscriptions.TryGetValue(connectionId, out var subscriptions) 
                ? subscriptions.ToArray() 
                : Array.Empty<LogLevel>();
        }
    }

    public IReadOnlyList<string> GetLevelSubscribers(LogLevel level)
    {
        lock (_lock)
        {
            var subscribers = new List<string>();
            
            foreach (var kvp in _levelSubscriptions)
            {
                if (kvp.Value.Contains(level))
                {
                    subscribers.Add(kvp.Key);
                }
            }
            
            return subscribers;
        }
    }
    #endregion

    #region 连接管理
    public Task ClearConnectionSubscriptionsAsync(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            throw new ArgumentNullException(nameof(connectionId));

        lock (_lock)
        {
            var removed = false;
            
            if (_categorySubscriptions.TryRemove(connectionId, out var categories))
            {
                removed = true;
                _logger.LogDebug("清理连接 {ConnectionId} 的类别订阅: {Categories}", 
                    connectionId, string.Join(", ", categories));
            }
            
            if (_levelSubscriptions.TryRemove(connectionId, out var levels))
            {
                removed = true;
                _logger.LogDebug("清理连接 {ConnectionId} 的级别订阅: {Levels}", 
                    connectionId, string.Join(", ", levels));
            }
            
            if (removed)
            {
                _connectionLastActivity.TryRemove(connectionId, out _);
                _logger.LogInformation("已清理连接 {ConnectionId} 的所有日志订阅", connectionId);
            }
        }
        
        return Task.CompletedTask;
    }

    public IReadOnlyList<string> GetAllConnections()
    {
        lock (_lock)
        {
            var connections = new HashSet<string>();
            
            foreach (var connectionId in _categorySubscriptions.Keys)
            {
                connections.Add(connectionId);
            }
            
            foreach (var connectionId in _levelSubscriptions.Keys)
            {
                connections.Add(connectionId);
            }
            
            return connections.ToArray();
        }
    }

    public Task<LogSubscriptionStats> GetConnectionStatsAsync(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            throw new ArgumentNullException(nameof(connectionId));

        lock (_lock)
        {
            var categories = GetCategorySubscriptions(connectionId);
            var levels = GetLevelSubscriptions(connectionId);
            var lastActivity = _connectionLastActivity.TryGetValue(connectionId, out var activity) 
                ? activity 
                : DateTime.UtcNow;

            var stats = new LogSubscriptionStats
            {
                ConnectionId = connectionId,
                Categories = categories,
                Levels = levels,
                LastActivityTime = lastActivity
            };

            return Task.FromResult(stats);
        }
    }
    #endregion

    #region 日志推送
    public IReadOnlyList<string> GetConnectionsForLog(LogCategory category, LogLevel level)
    {
        lock (_lock)
        {
            var connections = new HashSet<string>();
            
            // 获取订阅了该类别的连接
            foreach (var kvp in _categorySubscriptions)
            {
                if (kvp.Value.Contains(category))
                {
                    connections.Add(kvp.Key);
                }
            }
            
            // 获取订阅了该级别的连接
            foreach (var kvp in _levelSubscriptions)
            {
                if (kvp.Value.Contains(level))
                {
                    connections.Add(kvp.Key);
                }
            }
            
            return connections.ToArray();
        }
    }
    #endregion

    #region 私有方法
    private void UpdateLastActivity(string connectionId)
    {
        _connectionLastActivity[connectionId] = DateTime.UtcNow;
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
        lock (_lock)
        {
            _categorySubscriptions.Clear();
            _levelSubscriptions.Clear();
            _connectionLastActivity.Clear();
        }
        
        _logger.LogInformation("日志订阅管理器已释放");
    }
    #endregion
} 