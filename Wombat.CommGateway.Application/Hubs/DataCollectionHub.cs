using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Application.Services.DataCollection;
using Wombat.CommGateway.Domain.Enums;
using Wombat.Extensions.AutoGenerator.Attributes;

namespace Wombat.CommGateway.Application.Hubs
{
    /// <summary>
    /// 数据采集实时推送Hub
    /// 负责将缓存中的点位数据实时推送到前端
    /// 支持设备组、设备、点位三种订阅类型
    /// </summary>
    /// 
    //[AutoInject<DataCollectionHub>(ServiceLifetime = ServiceLifetime.Singleton)]
    public class DataCollectionHub : Hub
    {
        private readonly ILogger<DataCollectionHub> _logger;
        private readonly ICacheUpdateNotificationService _cacheUpdateNotificationService;
        private readonly ISubscriptionManager _subscriptionManager;

        // 如果你需要追踪连接 <-> 用户 Id，可继续保留
        private static readonly ConcurrentDictionary<string, string> _connectionUsers = new();

        public DataCollectionHub(
            ILogger<DataCollectionHub> logger,
            ISubscriptionManager subscriptionManager,
            ICacheUpdateNotificationService cacheUpdateNotificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _cacheUpdateNotificationService = cacheUpdateNotificationService ?? throw new ArgumentNullException(nameof(cacheUpdateNotificationService));
        }

        /************************** 连接生命周期 **************************/
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("客户端 {ConnectionId} 已连接", connectionId);

            // 先移除旧记录确保幂等，然后留空订阅集合
            _subscriptionManager.RemoveConnection(connectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("客户端 {ConnectionId} 已断开连接", connectionId);

            _subscriptionManager.RemoveConnection(connectionId);
            _connectionUsers.TryRemove(connectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        /************************** 订阅相关 **************************/
        /// <summary>
        /// 订阅设备
        /// 客户端将接收该设备下所有点位的数据更新
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        public async Task SubscribeDevice(int deviceId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.SubscribeToDevice(connectionId, deviceId);
            _logger.LogInformation("客户端 {ConnectionId} 订阅设备 {DeviceId}", connectionId, deviceId);

            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { Type = "Device", Id = deviceId });
        }

        /// <summary>
        /// 订阅设备组
        /// 客户端将接收该设备组下所有设备和点位的数据更新
        /// </summary>
        /// <param name="groupId">设备组ID</param>
        public async Task SubscribeGroup(int groupId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.SubscribeToGroup(connectionId, groupId);
            _logger.LogInformation("客户端 {ConnectionId} 订阅设备组 {GroupId}", connectionId, groupId);

            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { Type = "Group", Id = groupId });
        }

        /// <summary>
        /// 订阅点位
        /// 客户端将接收该点位的数据更新
        /// </summary>
        /// <param name="pointId">点位ID</param>
        public async Task SubscribePoint(int pointId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.SubscribeToPoint(connectionId, pointId);
            _logger.LogInformation("客户端 {ConnectionId} 订阅点位 {PointId}", connectionId, pointId);

            await Clients.Caller.SendAsync("SubscriptionConfirmed", new { Type = "Point", Id = pointId });
        }

        /// <summary>
        /// 取消订阅设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        public async Task UnsubscribeDevice(int deviceId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.UnsubscribeFromDevice(connectionId, deviceId);
            _logger.LogInformation("客户端 {ConnectionId} 取消订阅设备 {DeviceId}", connectionId, deviceId);

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new { Type = "Device", Id = deviceId });
        }

        /// <summary>
        /// 取消订阅设备组
        /// </summary>
        /// <param name="groupId">设备组ID</param>
        public async Task UnsubscribeGroup(int groupId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.UnsubscribeFromGroup(connectionId, groupId);
            _logger.LogInformation("客户端 {ConnectionId} 取消订阅设备组 {GroupId}", connectionId, groupId);

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new { Type = "Group", Id = groupId });
        }

        /// <summary>
        /// 取消订阅点位
        /// </summary>
        /// <param name="pointId">点位ID</param>
        public async Task UnsubscribePoint(int pointId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.UnsubscribeFromPoint(connectionId, pointId);
            _logger.LogInformation("客户端 {ConnectionId} 取消订阅点位 {PointId}", connectionId, pointId);

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", new { Type = "Point", Id = pointId });
        }

        /// <summary>
        /// 获取当前连接的订阅状态
        /// </summary>
        public async Task GetSubscriptionStatus()
        {
            var connectionId = Context.ConnectionId;
            var status = _subscriptionManager.GetConnectionStatus(connectionId);

            var statusData = new
            {
                ConnectionId = status.ConnectionId,
                TotalSubscriptions = status.TotalSubscriptions,
                GroupSubscriptions = status.GroupSubscriptions,
                DeviceSubscriptions = status.DeviceSubscriptions,
                PointSubscriptions = status.PointSubscriptions,
                LastActivityTime = status.LastActivityTime
            };

            await Clients.Caller.SendAsync("SubscriptionStatus", statusData);
        }

        /************************** 推送数据 **************************/
        /// <summary>
        /// 推送点位数据更新
        /// 自动发送给所有相关订阅者（直接订阅点位、订阅其设备、订阅其设备组的客户端）
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="value">点位值</param>
        /// <param name="status">点位状态</param>
        /// <param name="updateTime">更新时间</param>
        public async Task PushPointUpdate(int pointId, string value, DataPointStatus status, DateTime updateTime)
        {
            var message = new
            {
                Type = "PointUpdate",
                PointId = pointId,
                Value = value,
                Status = status.ToString(),
                UpdateTime = updateTime
            };

            // 使用优化的数据推送查询，一次性获取所有相关连接
            var subscribedConnections = _subscriptionManager.GetConnectionsForPointUpdate(pointId);
            if (subscribedConnections.Any())
            {
                await Clients.Clients(subscribedConnections).SendAsync("ReceivePointUpdate", message);
                _logger.LogDebug("推送点位 {PointId} 更新到 {Count} 个客户端（包括层级订阅）", pointId, subscribedConnections.Count);
            }
        }

        /// <summary>
        /// 推送批量点位更新
        /// </summary>
        /// <param name="updates">更新数据列表</param>
        public async Task PushBatchPointsUpdate(List<object> updates)
        {
            var message = new
            {
                Type = "BatchPointsUpdate",
                Updates = updates,
                UpdateTime = DateTime.UtcNow
            };

            await Clients.All.SendAsync("ReceiveBatchPointsUpdate", message);
            _logger.LogDebug("推送批量点位更新到所有客户端，共 {Count} 个点位", updates.Count);
        }

        /// <summary>
        /// 推送点位状态变更
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="status">新状态</param>
        public async Task PushPointStatusChange(int pointId, DataPointStatus status)
        {
            var message = new
            {
                Type = "PointStatusChange",
                PointId = pointId,
                Status = status.ToString(),
                UpdateTime = DateTime.UtcNow
            };

            // 使用优化的数据推送查询，包含层级订阅
            var subscribedConnections = _subscriptionManager.GetConnectionsForPointUpdate(pointId);
            if (subscribedConnections.Any())
            {
                await Clients.Clients(subscribedConnections).SendAsync("ReceivePointStatusChange", message);
                _logger.LogDebug("推送点位 {PointId} 状态变更到 {Count} 个客户端（包括层级订阅）", pointId, subscribedConnections.Count);
            }
        }

        /// <summary>
        /// 推送点位移除通知
        /// </summary>
        /// <param name="pointId">点位ID</param>
        public async Task PushPointRemoved(int pointId)
        {
            var message = new
            {
                Type = "PointRemoved",
                PointId = pointId,
                UpdateTime = DateTime.UtcNow
            };

            // 使用优化的数据推送查询，包含层级订阅
            var subscribedConnections = _subscriptionManager.GetConnectionsForPointUpdate(pointId);
            if (subscribedConnections.Any())
            {
                await Clients.Clients(subscribedConnections).SendAsync("ReceivePointRemoved", message);
                _logger.LogDebug("推送点位 {PointId} 移除通知到 {Count} 个客户端（包括层级订阅）", pointId, subscribedConnections.Count);
            }
        }

        /// <summary>
        /// 获取连接统计信息
        /// </summary>
        public async Task GetConnectionStatistics()
        {
            var allConnections = _subscriptionManager.GetAllConnections();
            var totalSubscriptions = 0;
            var groupSubscriptions = 0;
            var deviceSubscriptions = 0;
            var pointSubscriptions = 0;

            // 统计各类订阅数量
            foreach (var connectionId in allConnections)
            {
                var status = _subscriptionManager.GetConnectionStatus(connectionId);
                groupSubscriptions += status.GroupSubscriptions.Count;
                deviceSubscriptions += status.DeviceSubscriptions.Count;
                pointSubscriptions += status.PointSubscriptions.Count;
                totalSubscriptions += status.TotalSubscriptions;
            }

            var statistics = new
            {
                TotalConnections = allConnections.Count,
                TotalSubscriptions = totalSubscriptions,
                GroupSubscriptions = groupSubscriptions,
                DeviceSubscriptions = deviceSubscriptions,
                PointSubscriptions = pointSubscriptions,
                ConnectionIds = allConnections
            };

            await Clients.Caller.SendAsync("ConnectionStatistics", statistics);
        }

        /************************** 管理功能 **************************/
        /// <summary>
        /// 刷新层级关系缓存
        /// </summary>
        public async Task RefreshHierarchyCache()
        {
            try
            {
                await _subscriptionManager.RefreshHierarchyCacheAsync();
                _logger.LogInformation("层级关系缓存已刷新");
                await Clients.Caller.SendAsync("HierarchyCacheRefreshed", new { Success = true, Message = "层级关系缓存已刷新" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新层级关系缓存失败");
                await Clients.Caller.SendAsync("HierarchyCacheRefreshed", new { Success = false, Message = $"刷新失败：{ex.Message}" });
            }
        }
    }
} 