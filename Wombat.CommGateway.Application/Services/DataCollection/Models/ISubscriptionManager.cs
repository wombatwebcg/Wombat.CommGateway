using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    /// <summary>
    /// 订阅状态信息
    /// </summary>
    public class SubscriptionStatus
    {
        public string ConnectionId { get; set; }
        public IReadOnlyList<int> GroupSubscriptions { get; set; } = Array.Empty<int>();
        public IReadOnlyList<int> DeviceSubscriptions { get; set; } = Array.Empty<int>();
        public IReadOnlyList<int> PointSubscriptions { get; set; } = Array.Empty<int>();
        public DateTime LastActivityTime { get; set; }
        public int TotalSubscriptions => GroupSubscriptions.Count + DeviceSubscriptions.Count + PointSubscriptions.Count;
    }

    /// <summary>
    /// 订阅管理器接口
    /// 支持设备组、设备、点位三种订阅类型，提供层级关系缓存和高效数据推送
    /// </summary>
    public interface ISubscriptionManager : IDisposable
    {
        #region 设备组订阅
        /// <summary>
        /// 订阅设备组
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="groupId">设备组ID</param>
        void SubscribeToGroup(string connectionId, int groupId);

        /// <summary>
        /// 取消订阅设备组
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="groupId">设备组ID</param>
        void UnsubscribeFromGroup(string connectionId, int groupId);

        /// <summary>
        /// 获取连接的设备组订阅
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <returns>设备组ID列表</returns>
        IReadOnlyList<int> GetGroupSubscriptions(string connectionId);

        /// <summary>
        /// 获取订阅指定设备组的连接
        /// </summary>
        /// <param name="groupId">设备组ID</param>
        /// <returns>连接ID列表</returns>
        IReadOnlyList<string> GetGroupSubscribers(int groupId);
        #endregion

        #region 设备订阅
        /// <summary>
        /// 订阅设备
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="deviceId">设备ID</param>
        void SubscribeToDevice(string connectionId, int deviceId);

        /// <summary>
        /// 取消订阅设备
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="deviceId">设备ID</param>
        void UnsubscribeFromDevice(string connectionId, int deviceId);

        /// <summary>
        /// 获取连接的设备订阅
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <returns>设备ID列表</returns>
        IReadOnlyList<int> GetDeviceSubscriptions(string connectionId);

        /// <summary>
        /// 获取订阅指定设备的连接
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>连接ID列表</returns>
        IReadOnlyList<string> GetDeviceSubscribers(int deviceId);
        #endregion

        #region 点位订阅
        /// <summary>
        /// 订阅点位
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="pointId">点位ID</param>
        void SubscribeToPoint(string connectionId, int pointId);

        /// <summary>
        /// 取消订阅点位
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="pointId">点位ID</param>
        void UnsubscribeFromPoint(string connectionId, int pointId);

        /// <summary>
        /// 获取连接的点位订阅
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <returns>点位ID列表</returns>
        IReadOnlyList<int> GetPointSubscriptions(string connectionId);

        /// <summary>
        /// 获取订阅指定点位的连接
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>连接ID列表</returns>
        IReadOnlyList<string> GetPointSubscribers(int pointId);
        #endregion

        #region 连接管理
        /// <summary>
        /// 移除连接的所有订阅
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        void RemoveConnection(string connectionId);

        /// <summary>
        /// 获取所有连接ID
        /// </summary>
        /// <returns>连接ID列表</returns>
        IReadOnlyList<string> GetAllConnections();

        /// <summary>
        /// 获取连接的完整订阅状态
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <returns>订阅状态信息</returns>
        SubscriptionStatus GetConnectionStatus(string connectionId);
        #endregion

        #region 数据推送优化
        /// <summary>
        /// 获取点位数据更新时需要通知的所有连接
        /// 包括直接订阅该点位、订阅其设备、订阅其设备组的连接
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>需要通知的连接ID列表</returns>
        IReadOnlyList<string> GetConnectionsForPointUpdate(int pointId);

        /// <summary>
        /// 批量获取多个点位数据更新时需要通知的连接
        /// </summary>
        /// <param name="pointIds">点位ID列表</param>
        /// <returns>需要通知的连接ID列表（去重）</returns>
        IReadOnlyList<string> GetConnectionsForPointUpdates(IEnumerable<int> pointIds);
        #endregion

        #region 层级关系管理
        /// <summary>
        /// 刷新层级关系缓存
        /// </summary>
        Task RefreshHierarchyCacheAsync();

        /// <summary>
        /// 更新点位的层级关系
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="deviceId">设备ID</param>
        void UpdatePointHierarchy(int pointId, int deviceId);

        /// <summary>
        /// 更新设备的层级关系
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="groupId">设备组ID</param>
        void UpdateDeviceHierarchy(int deviceId, int groupId);

        /// <summary>
        /// 移除点位的层级关系
        /// </summary>
        /// <param name="pointId">点位ID</param>
        void RemovePointHierarchy(int pointId);

        /// <summary>
        /// 移除设备的层级关系
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        void RemoveDeviceHierarchy(int deviceId);
        #endregion

    }

    [AutoInject<ISubscriptionManager>(ServiceLifetime = ServiceLifetime.Singleton)]
    public class SubscriptionManager : ISubscriptionManager
    {
        #region 私有字段
        // 三种订阅类型的独立映射
        private readonly ConcurrentDictionary<string, HashSet<int>> _groupSubscriptions = new();
        private readonly ConcurrentDictionary<string, HashSet<int>> _deviceSubscriptions = new();
        private readonly ConcurrentDictionary<string, HashSet<int>> _pointSubscriptions = new();

        // 层级关系缓存
        private readonly ConcurrentDictionary<int, int> _pointToDevice = new();
        private readonly ConcurrentDictionary<int, int> _deviceToGroup = new();
        private readonly ConcurrentDictionary<int, HashSet<int>> _deviceToPoints = new();
        private readonly ConcurrentDictionary<int, HashSet<int>> _groupToDevices = new();

        // 连接活动时间跟踪
        private readonly ConcurrentDictionary<string, DateTime> _connectionLastActivity = new();

        // 读写锁，保护缓存一致性
        private readonly ReaderWriterLockSlim _cacheLock = new();

        // 依赖注入服务
        private readonly IServiceProvider _serviceProvider;
        
        // 日志记录器
        private readonly ILogger<SubscriptionManager> _logger;
        #endregion

        public SubscriptionManager(IServiceProvider serviceProvider, ILogger<SubscriptionManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        #region 设备组订阅实现
        public void SubscribeToGroup(string connectionId, int groupId)
        {
            if (string.IsNullOrEmpty(connectionId)) return;

            _groupSubscriptions.AddOrUpdate(
                connectionId,
                _ => new HashSet<int> { groupId },
                (_, set) => { lock (set) { set.Add(groupId); } return set; }
            );

            UpdateLastActivity(connectionId);
        }

        public void UnsubscribeFromGroup(string connectionId, int groupId)
        {
            if (string.IsNullOrEmpty(connectionId)) return;

            if (_groupSubscriptions.TryGetValue(connectionId, out var set))
            {
                lock (set)
                {
                    set.Remove(groupId);
                }
            }

            UpdateLastActivity(connectionId);
        }

        public IReadOnlyList<int> GetGroupSubscriptions(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId)) return Array.Empty<int>();

            return _groupSubscriptions.TryGetValue(connectionId, out var set)
                ? set.ToList() : Array.Empty<int>();
        }

        public IReadOnlyList<string> GetGroupSubscribers(int groupId)
        {
            return _groupSubscriptions
                .Where(kvp => kvp.Value.Contains(groupId))
                .Select(kvp => kvp.Key)
                .ToList();
        }
        #endregion

        #region 设备订阅实现
        public void SubscribeToDevice(string connectionId, int deviceId)
        {
            if (string.IsNullOrEmpty(connectionId)) return;

            _deviceSubscriptions.AddOrUpdate(
                connectionId,
                _ => new HashSet<int> { deviceId },
                (_, set) => { lock (set) { set.Add(deviceId); } return set; }
            );

            UpdateLastActivity(connectionId);
        }

        public void UnsubscribeFromDevice(string connectionId, int deviceId)
        {
            if (string.IsNullOrEmpty(connectionId)) return;

            if (_deviceSubscriptions.TryGetValue(connectionId, out var set))
            {
                lock (set)
                {
                    set.Remove(deviceId);
                }
            }

            UpdateLastActivity(connectionId);
        }

        public IReadOnlyList<int> GetDeviceSubscriptions(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId)) return Array.Empty<int>();

            return _deviceSubscriptions.TryGetValue(connectionId, out var set)
                ? set.ToList() : Array.Empty<int>();
        }

        public IReadOnlyList<string> GetDeviceSubscribers(int deviceId)
        {
            return _deviceSubscriptions
                .Where(kvp => kvp.Value.Contains(deviceId))
                .Select(kvp => kvp.Key)
                .ToList();
        }
        #endregion

        #region 点位订阅实现
        public void SubscribeToPoint(string connectionId, int pointId)
        {
            if (string.IsNullOrEmpty(connectionId)) return;

            _pointSubscriptions.AddOrUpdate(
                connectionId,
                _ => new HashSet<int> { pointId },
                (_, set) => { lock (set) { set.Add(pointId); } return set; }
            );

            UpdateLastActivity(connectionId);
        }

        public void UnsubscribeFromPoint(string connectionId, int pointId)
        {
            if (string.IsNullOrEmpty(connectionId)) return;

            if (_pointSubscriptions.TryGetValue(connectionId, out var set))
            {
                lock (set)
                {
                    set.Remove(pointId);
                }
            }

            UpdateLastActivity(connectionId);
        }

        public IReadOnlyList<int> GetPointSubscriptions(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId)) return Array.Empty<int>();

            return _pointSubscriptions.TryGetValue(connectionId, out var set)
                ? set.ToList() : Array.Empty<int>();
        }

        public IReadOnlyList<string> GetPointSubscribers(int pointId)
        {
            return _pointSubscriptions
                .Where(kvp => kvp.Value.Contains(pointId))
                .Select(kvp => kvp.Key)
                .ToList();
        }
        #endregion

        #region 连接管理实现
        public void RemoveConnection(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId)) return;

            _groupSubscriptions.TryRemove(connectionId, out _);
            _deviceSubscriptions.TryRemove(connectionId, out _);
            _pointSubscriptions.TryRemove(connectionId, out _);
            _connectionLastActivity.TryRemove(connectionId, out _);
        }

        public IReadOnlyList<string> GetAllConnections()
        {
            var connections = new HashSet<string>();
            
            foreach (var key in _groupSubscriptions.Keys) connections.Add(key);
            foreach (var key in _deviceSubscriptions.Keys) connections.Add(key);
            foreach (var key in _pointSubscriptions.Keys) connections.Add(key);

            return connections.ToList();
        }

        public SubscriptionStatus GetConnectionStatus(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                return new SubscriptionStatus { ConnectionId = connectionId };
            }

            return new SubscriptionStatus
            {
                ConnectionId = connectionId,
                GroupSubscriptions = GetGroupSubscriptions(connectionId),
                DeviceSubscriptions = GetDeviceSubscriptions(connectionId),
                PointSubscriptions = GetPointSubscriptions(connectionId),
                LastActivityTime = _connectionLastActivity.GetValueOrDefault(connectionId, DateTime.MinValue)
            };
        }
        #endregion

        #region 数据推送优化实现
        public IReadOnlyList<string> GetConnectionsForPointUpdate(int pointId)
        {
            var connections = new HashSet<string>();

            _cacheLock.EnterReadLock();
            try
            {
                // 1. 直接订阅该点位的连接
                var pointSubscribers = GetPointSubscribers(pointId);
                foreach (var conn in pointSubscribers)
                {
                    connections.Add(conn);
                }
                _logger?.LogDebug("点位 {PointId} 直接订阅者: {Count} 个", pointId, pointSubscribers.Count);

                // 2. 订阅该点位所属设备的连接
                if (_pointToDevice.TryGetValue(pointId, out var deviceId))
                {
                    var deviceSubscribers = GetDeviceSubscribers(deviceId);
                    foreach (var conn in deviceSubscribers)
                    {
                        connections.Add(conn);
                    }
                    _logger?.LogDebug("点位 {PointId} 所属设备 {DeviceId} 订阅者: {Count} 个", pointId, deviceId, deviceSubscribers.Count);

                    // 3. 订阅该点位所属设备组的连接
                    if (_deviceToGroup.TryGetValue(deviceId, out var groupId))
                    {
                        var groupSubscribers = GetGroupSubscribers(groupId);
                        foreach (var conn in groupSubscribers)
                        {
                            connections.Add(conn);
                        }
                        _logger?.LogDebug("点位 {PointId} 所属设备组 {GroupId} 订阅者: {Count} 个", pointId, groupId, groupSubscribers.Count);
                    }
                    else
                    {
                        _logger?.LogDebug("点位 {PointId} 所属设备 {DeviceId} 没有找到设备组映射", pointId, deviceId);
                    }
                }
                else
                {
                    _logger?.LogDebug("点位 {PointId} 没有找到设备映射", pointId);
                }

                _logger?.LogDebug("点位 {PointId} 总共找到 {Count} 个需要推送的连接", pointId, connections.Count);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            return connections.ToList();
        }

        public IReadOnlyList<string> GetConnectionsForPointUpdates(IEnumerable<int> pointIds)
        {
            var connections = new HashSet<string>();

            foreach (var pointId in pointIds)
            {
                var pointConnections = GetConnectionsForPointUpdate(pointId);
                foreach (var conn in pointConnections)
                {
                    connections.Add(conn);
                }
            }

            return connections.ToList();
        }
        #endregion

        #region 层级关系管理实现
        public async Task RefreshHierarchyCacheAsync()
        {
            _logger?.LogInformation("开始刷新层级关系缓存...");
            
            _cacheLock.EnterWriteLock();
            try
            {
                // 清空现有缓存
                _pointToDevice.Clear();
                _deviceToGroup.Clear();
                _deviceToPoints.Clear();
                _groupToDevices.Clear();

                // 从数据库重新加载层级关系
                using var scope = _serviceProvider.CreateScope();
                var devicePointRepository = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IDevicePointRepository>();
                var deviceRepository = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IDeviceRepository>();

                var points = await devicePointRepository.GetAllAsync();
                _logger?.LogInformation("加载了 {Count} 个点位到层级缓存", points.Count());
                foreach (var point in points)
                {
                    UpdatePointHierarchyInternal(point.Id, point.DeviceId);
                }

                var devices = await deviceRepository.GetAllAsync();
                _logger?.LogInformation("加载了 {Count} 个设备到层级缓存", devices.Count);
                foreach (var device in devices)
                {
                    UpdateDeviceHierarchyInternal(device.Id, device.DeviceGroupId);
                }
                
                _logger?.LogInformation("层级关系缓存刷新完成 - 点位映射: {PointCount}, 设备映射: {DeviceCount}", 
                    _pointToDevice.Count, _deviceToGroup.Count);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public void UpdatePointHierarchy(int pointId, int deviceId)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                UpdatePointHierarchyInternal(pointId, deviceId);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public void UpdateDeviceHierarchy(int deviceId, int groupId)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                UpdateDeviceHierarchyInternal(deviceId, groupId);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public void RemovePointHierarchy(int pointId)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                if (_pointToDevice.TryRemove(pointId, out var deviceId))
                {
                    if (_deviceToPoints.TryGetValue(deviceId, out var points))
                    {
                        lock (points)
                        {
                            points.Remove(pointId);
                        }
                    }
                }
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }

        public void RemoveDeviceHierarchy(int deviceId)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                if (_deviceToGroup.TryRemove(deviceId, out var groupId))
                {
                    if (_groupToDevices.TryGetValue(groupId, out var devices))
                    {
                        lock (devices)
                        {
                            devices.Remove(deviceId);
                        }
                    }
                }

                _deviceToPoints.TryRemove(deviceId, out _);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        #endregion

        #region 向后兼容实现
        [Obsolete("请使用 SubscribeToPoint、SubscribeToDevice 或 SubscribeToGroup 方法")]
        public void Add(string connectionKey, int itemId)
        {
            // 为了向后兼容，默认当作点位订阅处理
            SubscribeToPoint(connectionKey, itemId);
        }

        [Obsolete("请使用 UnsubscribeFromPoint、UnsubscribeFromDevice 或 UnsubscribeFromGroup 方法")]
        public void Remove(string connectionKey, int itemId)
        {
            // 为了向后兼容，尝试从所有类型中移除
            UnsubscribeFromPoint(connectionKey, itemId);
            UnsubscribeFromDevice(connectionKey, itemId);
            UnsubscribeFromGroup(connectionKey, itemId);
        }

        [Obsolete("请使用 GetPointSubscriptions、GetDeviceSubscriptions 或 GetGroupSubscriptions 方法")]
        public IReadOnlyList<int> Get(string connectionKey)
        {
            // 为了向后兼容，返回点位订阅
            return GetPointSubscriptions(connectionKey);
        }

        [Obsolete("请使用 GetPointSubscribers、GetDeviceSubscribers 或 GetGroupSubscribers 方法")]
        public IReadOnlyList<string> GetConnectionsByItem(int itemId)
        {
            // 为了向后兼容，当作点位处理
            return GetPointSubscribers(itemId);
        }
        #endregion

        #region 私有辅助方法
        private void UpdateLastActivity(string connectionId)
        {
            _connectionLastActivity.AddOrUpdate(connectionId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
        }

        private void UpdatePointHierarchyInternal(int pointId, int deviceId)
        {
            _pointToDevice[pointId] = deviceId;
            
            _deviceToPoints.AddOrUpdate(
                deviceId,
                _ => new HashSet<int> { pointId },
                (_, set) => { lock (set) { set.Add(pointId); } return set; }
            );
        }

        private void UpdateDeviceHierarchyInternal(int deviceId, int groupId)
        {
            _deviceToGroup[deviceId] = groupId;
            
            _groupToDevices.AddOrUpdate(
                groupId,
                _ => new HashSet<int> { deviceId },
                (_, set) => { lock (set) { set.Add(deviceId); } return set; }
            );
        }
        #endregion

        #region 资源释放
        public void Dispose()
        {
            _cacheLock?.Dispose();
        }
        #endregion
    }
}
