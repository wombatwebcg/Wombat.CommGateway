using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;
using Wombat.Extensions.AutoGenerator.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Wombat.CommGateway.Application.Interfaces;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    /// <summary>
    /// 数据缓存管理器
    /// 负责缓存采集到的数据，减少数据库访问
    /// </summary>
    [AutoInject(typeof(CacheManager),ServiceLifetime.Singleton)]
    public class CacheManager
    {
        private readonly ILogger<CacheManager> _logger;
        private readonly ICacheUpdateNotificationService _notificationService;
        private readonly ConcurrentDictionary<int, CachedPoint> _pointCache;
        private readonly Timer _flushTimer;
        private readonly int _flushIntervalMs;
        private readonly int _maxCacheAge;
        private readonly object _lockObject = new object();
        private readonly ConcurrentQueue<int> _dirtyPoints;
        private volatile bool _isRunning;

        public CacheManager(ILogger<CacheManager> logger, ICacheUpdateNotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _pointCache = new ConcurrentDictionary<int, CachedPoint>();
            _dirtyPoints = new ConcurrentQueue<int>();
            _flushIntervalMs = 5000; // 5秒刷新一次
            _maxCacheAge = 60000; // 最大缓存时间1分钟
            _flushTimer = new Timer(FlushCache, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// 启动缓存管理器
        /// </summary>
        public void Start()
        {
            lock (_lockObject)
            {
                if (_isRunning)
                    return;

                _isRunning = true;
                _flushTimer.Change(0, _flushIntervalMs);
                _logger.LogInformation("缓存管理器已启动");
            }
        }

        /// <summary>
        /// 停止缓存管理器
        /// </summary>
        public void Stop()
        {
            lock (_lockObject)
            {
                if (!_isRunning)
                    return;

                _isRunning = false;
                _flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _logger.LogInformation("缓存管理器已停止");
            }
        }

        /// <summary>
        /// 更新点位缓存
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="value">点位值</param>
        /// <param name="status">点位状态</param>
        public void UpdateCache(int pointId, string value, DataPointStatus status = DataPointStatus.Good)
        {
            var now = DateTime.Now;
            var cachedPoint = _pointCache.GetOrAdd(pointId, id => new CachedPoint
            {
                PointId = id,
                Value = value,
                Status = status,
                UpdateTime = now,
                LastFlushTime = DateTime.MinValue,
                IsDirty = true
            });

            // 更新现有缓存
            if (cachedPoint.Value != value || cachedPoint.Status != status)
            {
                cachedPoint.Value = value;
                cachedPoint.Status = status;
                cachedPoint.UpdateTime = now;
                cachedPoint.IsDirty = true;

                // 添加到脏数据队列
                _dirtyPoints.Enqueue(pointId);

                // 通知WebSocket服务推送数据更新
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.OnPointDataUpdatedAsync(pointId, value, status, now);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"通知点位 {pointId} 数据更新时发生错误");
                    }
                });
            }
        }

        /// <summary>
        /// 批量更新点位缓存
        /// </summary>
        /// <param name="updates">点位更新字典</param>
        public void BatchUpdateCache(Dictionary<int, (string Value, DataPointStatus Status)> updates)
        {
            if (updates == null || updates.Count == 0)
                return;

            var now = DateTime.Now;
            foreach (var kvp in updates)
            {
                var pointId = kvp.Key;
                var (value, status) = kvp.Value;

                var cachedPoint = _pointCache.GetOrAdd(pointId, id => new CachedPoint
                {
                    PointId = id,
                    Value = value,
                    Status = status,
                    UpdateTime = now,
                    LastFlushTime = DateTime.MinValue,
                    IsDirty = true
                });

                // 更新现有缓存
                if (cachedPoint.Value != value || cachedPoint.Status != status)
                {
                    cachedPoint.Value = value;
                    cachedPoint.Status = status;
                    cachedPoint.UpdateTime = now;
                    cachedPoint.IsDirty = true;

                    // 添加到脏数据队列
                    _dirtyPoints.Enqueue(pointId);
                }
            }

            // 批量通知WebSocket服务推送数据更新
            _ = Task.Run(async () =>
            {
                try
                {
                    var batchUpdates = new Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)>();
                    foreach (var kvp in updates)
                    {
                        batchUpdates[kvp.Key] = (kvp.Value.Value, kvp.Value.Status, now);
                    }
                    await _notificationService.OnBatchPointsDataUpdatedAsync(batchUpdates);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"批量通知点位数据更新时发生错误");
                }
            });
        }

        /// <summary>
        /// 获取缓存的点位值
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>缓存的点位值</returns>
        public (string Value, DataPointStatus Status, DateTime UpdateTime)? GetCachedValue(int pointId)
        {
            if (_pointCache.TryGetValue(pointId, out var cachedPoint))
            {
                return (cachedPoint.Value, cachedPoint.Status, cachedPoint.UpdateTime);
            }
            return null;
        }

        /// <summary>
        /// 批量获取缓存的点位值
        /// </summary>
        /// <param name="pointIds">点位ID列表</param>
        /// <returns>点位缓存字典</returns>
        public Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)> BatchGetCachedValues(IEnumerable<int> pointIds)
        {
            var result = new Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)>();
            foreach (var pointId in pointIds)
            {
                if (_pointCache.TryGetValue(pointId, out var cachedPoint))
                {
                    result[pointId] = (cachedPoint.Value, cachedPoint.Status, cachedPoint.UpdateTime);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取所有脏数据
        /// </summary>
        /// <returns>脏数据字典</returns>
        public Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)> GetAllDirtyData()
        {
            var result = new Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)>();
            var processedIds = new HashSet<int>();

            // 从脏数据队列中获取
            while (_dirtyPoints.TryDequeue(out var pointId))
            {
                if (processedIds.Contains(pointId))
                    continue;

                if (_pointCache.TryGetValue(pointId, out var cachedPoint) && cachedPoint.IsDirty)
                {
                    result[pointId] = (cachedPoint.Value, cachedPoint.Status, cachedPoint.UpdateTime);
                    processedIds.Add(pointId);
                }
            }

            return result;
        }

        /// <summary>
        /// 将脏数据标记为已刷新
        /// </summary>
        /// <param name="pointIds">已刷新的点位ID列表</param>
        public void MarkAsFlushed(IEnumerable<int> pointIds)
        {
            var now = DateTime.Now;
            foreach (var pointId in pointIds)
            {
                if (_pointCache.TryGetValue(pointId, out var cachedPoint))
                {
                    cachedPoint.IsDirty = false;
                    cachedPoint.LastFlushTime = now;
                }
            }
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        public void CleanupExpiredCache()
        {
            var now = DateTime.Now;
            var expiredPoints = new List<int>();

            foreach (var kvp in _pointCache)
            {
                var pointId = kvp.Key;
                var cachedPoint = kvp.Value;

                // 如果缓存已过期且不是脏数据
                if (!cachedPoint.IsDirty && (now - cachedPoint.LastFlushTime).TotalMilliseconds > _maxCacheAge)
                {
                    expiredPoints.Add(pointId);
                }
            }

            foreach (var pointId in expiredPoints)
            {
                _pointCache.TryRemove(pointId, out _);
            }

            if (expiredPoints.Count > 0)
            {
                _logger.LogDebug($"已清理 {expiredPoints.Count} 个过期缓存");
            }
        }

        private void FlushCache(object state)
        {
            if (!_isRunning)
                return;

            try
            {
                // 这里只是触发刷新事件，实际的刷新操作由外部处理
                var dirtyData = GetAllDirtyData();
                if (dirtyData.Count > 0)
                {
                    _logger.LogInformation($"触发刷新 {dirtyData.Count} 个脏数据");
                    OnFlushRequired?.Invoke(dirtyData);
                }

                // 清理过期缓存
                CleanupExpiredCache();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新缓存时发生错误");
            }
        }

        /// <summary>
        /// 缓存刷新事件
        /// </summary>
        public event Action<Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)>> OnFlushRequired;

        /// <summary>
        /// 从缓存中移除指定点位
        /// </summary>
        /// <param name="pointId">要移除的点位ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveFromCache(int pointId)
        {
            var removed = _pointCache.TryRemove(pointId, out _);
            if (removed)
            {
                _logger.LogInformation($"已从缓存中移除点位 {pointId}");
            }
            return removed;
        }

        /// <summary>
        /// 批量从缓存中移除指定点位
        /// </summary>
        /// <param name="pointIds">要移除的点位ID列表</param>
        /// <returns>成功移除的点位数量</returns>
        public int BatchRemoveFromCache(IEnumerable<int> pointIds)
        {
            if (pointIds == null)
                return 0;

            int count = 0;
            foreach (var pointId in pointIds)
            {
                if (RemoveFromCache(pointId))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 清理所有缓存数据
        /// </summary>
        public void ClearAllCache()
        {
            _logger.LogInformation("清理所有点位缓存数据...");
            _pointCache.Clear();
            
            // 清理脏数据队列
            while (_dirtyPoints.TryDequeue(out _)) { }
            
            _logger.LogInformation("所有点位缓存数据已清理完成");
        }
    }
} 