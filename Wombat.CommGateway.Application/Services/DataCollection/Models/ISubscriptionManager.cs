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
    /// 点位订阅详情信息
    /// </summary>
    public class PointSubscriptionDetails
    {
        public int PointId { get; set; }
        public string? ConnectionId { get; set; }
        public int? DeviceId { get; set; }
        public int? GroupId { get; set; }
        
        // 各层级的订阅者
        public List<string> DirectPointSubscribers { get; set; } = new();
        public List<string> DeviceSubscribers { get; set; } = new();
        public List<string> GroupSubscribers { get; set; } = new();
        public List<string> AllSubscribers { get; set; } = new();
        
        // 特定连接的订阅状态（当提供ConnectionId时）
        public bool IsDirectPointSubscriber { get; set; }
        public bool IsDeviceSubscriber { get; set; }
        public bool IsGroupSubscriber { get; set; }
        public bool WillReceiveUpdates { get; set; }
        public List<string> SubscriptionReasons { get; set; } = new();
        
        // 统计信息
        public int TotalSubscriberCount => AllSubscribers.Count;
        public int DirectSubscriberCount => DirectPointSubscribers.Count;
        public int DeviceSubscriberCount => DeviceSubscribers.Count;
        public int GroupSubscriberCount => GroupSubscribers.Count;
    }

    /// <summary>
    /// 点位数据快照
    /// </summary>
    public class PointDataSnapshot
    {
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UpdateTime { get; set; }
        public bool Enable { get; set; }
        public int ScanRate { get; set; }
    }

    /// <summary>
    /// 设备数据快照
    /// </summary>
    public class DeviceDataSnapshot
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public bool DeviceEnable { get; set; }
        public int ChannelId { get; set; }
        public List<PointDataSnapshot> Points { get; set; } = new();
        public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 设备组数据快照
    /// </summary>
    public class GroupDataSnapshot
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<DeviceDataSnapshot> Devices { get; set; } = new();
        public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
        public int TotalPointCount => Devices.Sum(d => d.Points.Count);
        public int TotalDeviceCount => Devices.Count;
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

        /// <summary>
        /// 获取点位订阅详情，用于诊断为什么某个连接会收到特定点位的推送
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="connectionId">连接ID（可选，如果提供则只分析该连接）</param>
        /// <returns>详细的订阅路径信息</returns>
        PointSubscriptionDetails GetPointSubscriptionDetails(int pointId, string? connectionId = null);

        #region 主动数据推送
        /// <summary>
        /// 获取设备下所有点位的当前数据
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>设备下所有点位的数据</returns>
        Task<DeviceDataSnapshot> GetDeviceDataSnapshotAsync(int deviceId);

        /// <summary>
        /// 获取设备组下所有点位的当前数据
        /// </summary>
        /// <param name="groupId">设备组ID</param>
        /// <returns>设备组下所有点位的数据</returns>
        Task<GroupDataSnapshot> GetGroupDataSnapshotAsync(int groupId);

        /// <summary>
        /// 获取设备下所有点位ID列表
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>点位ID列表</returns>
        IReadOnlyList<int> GetDevicePointIds(int deviceId);

        /// <summary>
        /// 获取设备组下所有点位ID列表
        /// </summary>
        /// <param name="groupId">设备组ID</param>
        /// <returns>点位ID列表</returns>
        IReadOnlyList<int> GetGroupPointIds(int groupId);
        #endregion

        #region 诊断和维护
        /// <summary>
        /// 获取所有连接的详细订阅信息
        /// </summary>
        /// <returns>所有连接的订阅详情</returns>
        List<ConnectionSubscriptionDetails> GetAllConnectionDetails();

        /// <summary>
        /// 强制清理指定连接的特定类型订阅
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="subscriptionType">订阅类型：device, group, point, all</param>
        /// <param name="targetId">目标ID（当subscriptionType不是all时）</param>
        /// <returns>清理结果</returns>
        CleanupResult ForceCleanupSubscription(string connectionId, string subscriptionType, int? targetId = null);
        #endregion
    }

    /// <summary>
    /// 连接订阅详情
    /// </summary>
    public class ConnectionSubscriptionDetails
    {
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime LastActivityTime { get; set; }
        public List<int> GroupSubscriptions { get; set; } = new();
        public List<int> DeviceSubscriptions { get; set; } = new();
        public List<int> PointSubscriptions { get; set; } = new();
        public int TotalSubscriptions => GroupSubscriptions.Count + DeviceSubscriptions.Count + PointSubscriptions.Count;
    }

    /// <summary>
    /// 清理结果
    /// </summary>
    public class CleanupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RemovedCount { get; set; }
        public List<string> Details { get; set; } = new();
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
            if (string.IsNullOrEmpty(connectionId)) 
            {
                _logger?.LogWarning("取消订阅设备失败：连接ID为空");
                return;
            }

            _logger?.LogDebug("开始取消订阅设备：连接ID={ConnectionId}, 设备ID={DeviceId}", connectionId, deviceId);

            bool removed = false;
            if (_deviceSubscriptions.TryGetValue(connectionId, out var set))
            {
                lock (set)
                {
                    removed = set.Remove(deviceId);
                    _logger?.LogDebug("设备订阅移除结果：连接ID={ConnectionId}, 设备ID={DeviceId}, 移除成功={Removed}, 剩余订阅数={Count}", 
                        connectionId, deviceId, removed, set.Count);
                }
            }
            else
            {
                _logger?.LogWarning("取消订阅设备失败：连接ID={ConnectionId} 没有找到设备订阅记录", connectionId);
            }

            if (removed)
            {
                _logger?.LogInformation("成功取消订阅设备：连接ID={ConnectionId}, 设备ID={DeviceId}", connectionId, deviceId);
            }
            else
            {
                _logger?.LogWarning("取消订阅设备失败：连接ID={ConnectionId}, 设备ID={DeviceId} 不在订阅列表中", connectionId, deviceId);
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

        /// <summary>
        /// 获取点位订阅详情，用于诊断为什么某个连接会收到特定点位的推送
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="connectionId">连接ID（可选，如果提供则只分析该连接）</param>
        /// <returns>详细的订阅路径信息</returns>
        public PointSubscriptionDetails GetPointSubscriptionDetails(int pointId, string? connectionId = null)
        {
            var details = new PointSubscriptionDetails
            {
                PointId = pointId,
                ConnectionId = connectionId
            };

            _cacheLock.EnterReadLock();
            try
            {
                // 1. 获取直接订阅该点位的连接
                var pointSubscribers = GetPointSubscribers(pointId);
                details.DirectPointSubscribers = pointSubscribers.ToList();
                
                if (!string.IsNullOrEmpty(connectionId))
                {
                    details.IsDirectPointSubscriber = pointSubscribers.Contains(connectionId);
                }

                // 2. 获取该点位所属设备信息
                if (_pointToDevice.TryGetValue(pointId, out var deviceId))
                {
                    details.DeviceId = deviceId;
                    var deviceSubscribers = GetDeviceSubscribers(deviceId);
                    details.DeviceSubscribers = deviceSubscribers.ToList();
                    
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        details.IsDeviceSubscriber = deviceSubscribers.Contains(connectionId);
                    }

                    // 3. 获取该设备所属设备组信息
                    if (_deviceToGroup.TryGetValue(deviceId, out var groupId))
                    {
                        details.GroupId = groupId;
                        var groupSubscribers = GetGroupSubscribers(groupId);
                        details.GroupSubscribers = groupSubscribers.ToList();
                        
                        if (!string.IsNullOrEmpty(connectionId))
                        {
                            details.IsGroupSubscriber = groupSubscribers.Contains(connectionId);
                        }
                    }
                }

                // 4. 汇总所有会收到该点位推送的连接
                var allSubscribers = new HashSet<string>();
                allSubscribers.UnionWith(details.DirectPointSubscribers);
                allSubscribers.UnionWith(details.DeviceSubscribers);
                allSubscribers.UnionWith(details.GroupSubscribers);
                details.AllSubscribers = allSubscribers.ToList();

                // 5. 如果指定了连接ID，分析该连接的订阅原因
                if (!string.IsNullOrEmpty(connectionId))
                {
                    var reasons = new List<string>();
                    if (details.IsDirectPointSubscriber)
                        reasons.Add($"直接订阅点位 {pointId}");
                    if (details.IsDeviceSubscriber && details.DeviceId.HasValue)
                        reasons.Add($"订阅设备 {details.DeviceId.Value}（包含点位 {pointId}）");
                    if (details.IsGroupSubscriber && details.GroupId.HasValue)
                        reasons.Add($"订阅设备组 {details.GroupId.Value}（包含设备 {details.DeviceId}，进而包含点位 {pointId}）");
                    
                    details.SubscriptionReasons = reasons;
                    details.WillReceiveUpdates = reasons.Count > 0;
                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            return details;
        }

        #region 主动数据推送实现
        /// <summary>
        /// 获取设备下所有点位的当前数据
        /// </summary>
        public async Task<DeviceDataSnapshot> GetDeviceDataSnapshotAsync(int deviceId)
        {
            var snapshot = new DeviceDataSnapshot
            {
                DeviceId = deviceId,
                SnapshotTime = DateTime.UtcNow
            };

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var deviceRepository = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IDeviceRepository>();
                var devicePointRepository = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IDevicePointRepository>();
                var cacheManager = scope.ServiceProvider.GetRequiredService<CacheManager>();

                // 获取设备信息
                var device = await deviceRepository.GetByIdAsync(deviceId);
                if (device != null)
                {
                    snapshot.DeviceName = device.Name;
                    snapshot.DeviceEnable = device.Enable;
                    snapshot.ChannelId = device.ChannelId;
                }

                // 获取设备下所有点位
                var points = await devicePointRepository.GetDevicePointsAsync(deviceId);
                foreach (var point in points)
                {
                    var pointSnapshot = new PointDataSnapshot
                    {
                        PointId = point.Id,
                        Name = point.Name,
                        Address = point.Address,
                        Enable = point.Enable,
                        ScanRate = point.ScanRate
                    };

                    // 尝试从缓存获取最新值
                    var cachedValue = cacheManager.GetCachedValue(point.Id);
                    if (cachedValue.HasValue)
                    {
                        pointSnapshot.Value = cachedValue.Value.Value;
                        pointSnapshot.Status = cachedValue.Value.Status.ToString();
                        pointSnapshot.UpdateTime = cachedValue.Value.UpdateTime;
                    }
                    else
                    {
                        // 使用数据库中的值
                        pointSnapshot.Value = point.Value ?? string.Empty;
                        pointSnapshot.Status = point.Status.ToString();
                        pointSnapshot.UpdateTime = point.UpdateTime;
                    }

                    snapshot.Points.Add(pointSnapshot);
                }

                _logger?.LogDebug("获取设备 {DeviceId} 数据快照完成，包含 {PointCount} 个点位", deviceId, snapshot.Points.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取设备 {DeviceId} 数据快照时发生错误", deviceId);
            }

            return snapshot;
        }

        /// <summary>
        /// 获取设备组下所有点位的当前数据
        /// </summary>
        public async Task<GroupDataSnapshot> GetGroupDataSnapshotAsync(int groupId)
        {
            var snapshot = new GroupDataSnapshot
            {
                GroupId = groupId,
                SnapshotTime = DateTime.UtcNow
            };

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var deviceRepository = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IDeviceRepository>();
                var deviceGroupRepository = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IDeviceGroupRepository>();

                // 获取设备组信息
                var group = await deviceGroupRepository.GetByIdAsync(groupId);
                if (group != null)
                {
                    snapshot.GroupName = group.Name;
                }

                // 获取设备组下所有设备
                var devices = await deviceRepository.Select.Where(x=>x.DeviceGroupId == groupId).ToListAsync();
                foreach (var device in devices)
                {
                    var deviceSnapshot = await GetDeviceDataSnapshotAsync(device.Id);
                    snapshot.Devices.Add(deviceSnapshot);
                }

                _logger?.LogDebug("获取设备组 {GroupId} 数据快照完成，包含 {DeviceCount} 个设备，{PointCount} 个点位", 
                    groupId, snapshot.TotalDeviceCount, snapshot.TotalPointCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取设备组 {GroupId} 数据快照时发生错误", groupId);
            }

            return snapshot;
        }

        /// <summary>
        /// 获取设备下所有点位ID列表
        /// </summary>
        public IReadOnlyList<int> GetDevicePointIds(int deviceId)
        {
            _cacheLock.EnterReadLock();
            try
            {
                if (_deviceToPoints.TryGetValue(deviceId, out var pointIds))
                {
                    return pointIds.ToList();
                }
                return Array.Empty<int>();
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 获取设备组下所有点位ID列表
        /// </summary>
        public IReadOnlyList<int> GetGroupPointIds(int groupId)
        {
            var pointIds = new List<int>();
            
            _cacheLock.EnterReadLock();
            try
            {
                // 获取设备组下所有设备
                if (_groupToDevices.TryGetValue(groupId, out var deviceIds))
                {
                    foreach (var deviceId in deviceIds)
                    {
                        // 获取每个设备下的点位
                        if (_deviceToPoints.TryGetValue(deviceId, out var devicePointIds))
                        {
                            pointIds.AddRange(devicePointIds);
                        }
                    }
                }
                
                return pointIds;
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }
        #endregion

        #region 诊断和维护
        /// <summary>
        /// 获取所有连接的详细订阅信息
        /// </summary>
        /// <returns>所有连接的订阅详情</returns>
        public List<ConnectionSubscriptionDetails> GetAllConnectionDetails()
        {
            _cacheLock.EnterReadLock();
            try
            {
                var connectionMap = new Dictionary<string, ConnectionSubscriptionDetails>();
                
                // 收集所有连接ID
                var allConnections = new HashSet<string>();
                foreach (var key in _groupSubscriptions.Keys) allConnections.Add(key);
                foreach (var key in _deviceSubscriptions.Keys) allConnections.Add(key);
                foreach (var key in _pointSubscriptions.Keys) allConnections.Add(key);
                
                // 为每个连接创建详情对象
                foreach (var connectionId in allConnections)
                {
                    var detail = new ConnectionSubscriptionDetails
                    {
                        ConnectionId = connectionId,
                        LastActivityTime = _connectionLastActivity.GetValueOrDefault(connectionId, DateTime.MinValue)
                    };
                    
                    // 添加组订阅
                    if (_groupSubscriptions.TryGetValue(connectionId, out var groupSet))
                    {
                        detail.GroupSubscriptions = groupSet.ToList();
                    }
                    
                    // 添加设备订阅
                    if (_deviceSubscriptions.TryGetValue(connectionId, out var deviceSet))
                    {
                        detail.DeviceSubscriptions = deviceSet.ToList();
                    }
                    
                    // 添加点位订阅
                    if (_pointSubscriptions.TryGetValue(connectionId, out var pointSet))
                    {
                        detail.PointSubscriptions = pointSet.ToList();
                    }
                    
                    connectionMap[connectionId] = detail;
                }
                
                return connectionMap.Values.ToList();
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 强制清理指定连接的特定类型订阅
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="subscriptionType">订阅类型：device, group, point, all</param>
        /// <param name="targetId">目标ID（当subscriptionType不是all时）</param>
        /// <returns>清理结果</returns>
        public CleanupResult ForceCleanupSubscription(string connectionId, string subscriptionType, int? targetId = null)
        {
            _cacheLock.EnterWriteLock();
            try
            {
                var removedCount = 0;
                var details = new List<string>();

                if (string.IsNullOrEmpty(connectionId))
                {
                    return new CleanupResult { Success = false, Message = "连接ID不能为空", RemovedCount = 0, Details = new List<string> { "连接ID为空" } };
                }

                if (subscriptionType == "all")
                {
                    _groupSubscriptions.TryRemove(connectionId, out _);
                    _deviceSubscriptions.TryRemove(connectionId, out _);
                    _pointSubscriptions.TryRemove(connectionId, out _);
                    _connectionLastActivity.TryRemove(connectionId, out _);
                    removedCount = 3; // 移除所有订阅
                    details.Add($"移除连接 {connectionId} 的所有订阅");
                }
                else if (subscriptionType == "group")
                {
                    if (targetId.HasValue)
                    {
                        _groupSubscriptions.TryGetValue(connectionId, out var set);
                        if (set != null)
                        {
                            lock (set)
                            {
                                removedCount = set.Remove(targetId.Value) ? 1 : 0;
                            }
                            if (removedCount > 0)
                            {
                                details.Add($"移除连接 {connectionId} 对设备组 {targetId.Value} 的订阅");
                            }
                        }
                    }
                    else
                    {
                        _groupSubscriptions.TryRemove(connectionId, out _);
                        _connectionLastActivity.TryRemove(connectionId, out _);
                        removedCount = 2; // 移除组订阅和活动时间
                        details.Add($"移除连接 {connectionId} 的所有组订阅");
                    }
                }
                else if (subscriptionType == "device")
                {
                    if (targetId.HasValue)
                    {
                        _deviceSubscriptions.TryGetValue(connectionId, out var set);
                        if (set != null)
                        {
                            lock (set)
                            {
                                removedCount = set.Remove(targetId.Value) ? 1 : 0;
                            }
                            if (removedCount > 0)
                            {
                                details.Add($"移除连接 {connectionId} 对设备 {targetId.Value} 的订阅");
                            }
                        }
                    }
                    else
                    {
                        _deviceSubscriptions.TryRemove(connectionId, out _);
                        _connectionLastActivity.TryRemove(connectionId, out _);
                        removedCount = 2; // 移除设备订阅和活动时间
                        details.Add($"移除连接 {connectionId} 的所有设备订阅");
                    }
                }
                else if (subscriptionType == "point")
                {
                    if (targetId.HasValue)
                    {
                        _pointSubscriptions.TryGetValue(connectionId, out var set);
                        if (set != null)
                        {
                            lock (set)
                            {
                                removedCount = set.Remove(targetId.Value) ? 1 : 0;
                            }
                            if (removedCount > 0)
                            {
                                details.Add($"移除连接 {connectionId} 对点位 {targetId.Value} 的订阅");
                            }
                        }
                    }
                    else
                    {
                        _pointSubscriptions.TryRemove(connectionId, out _);
                        _connectionLastActivity.TryRemove(connectionId, out _);
                        removedCount = 2; // 移除点位订阅和活动时间
                        details.Add($"移除连接 {connectionId} 的所有点位订阅");
                    }
                }
                else
                {
                    return new CleanupResult { Success = false, Message = $"不支持的订阅类型: {subscriptionType}", RemovedCount = 0, Details = new List<string> { $"不支持的订阅类型: {subscriptionType}" } };
                }

                _connectionLastActivity.AddOrUpdate(connectionId, DateTime.UtcNow, (_, _) => DateTime.UtcNow); // 更新活动时间
                return new CleanupResult { Success = true, Message = $"成功清理连接 {connectionId} 的 {subscriptionType} 订阅", RemovedCount = removedCount, Details = details };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "强制清理订阅失败: 连接ID={ConnectionId}, 类型={SubscriptionType}, 目标ID={TargetId}", connectionId, subscriptionType, targetId);
                return new CleanupResult { Success = false, Message = $"强制清理订阅失败: {ex.Message}", RemovedCount = 0, Details = new List<string> { $"强制清理订阅失败: {ex.Message}" } };
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        #endregion
    }
}
