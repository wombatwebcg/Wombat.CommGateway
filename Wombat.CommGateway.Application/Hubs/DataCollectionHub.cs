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
        public async Task SubscribeDevice(int deviceId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.Add(connectionId, deviceId);
            _logger.LogInformation("客户端 {ConnectionId} 订阅设备 {DeviceId}", connectionId, deviceId);

            await Clients.Caller.SendAsync("SubscriptionConfirmed", deviceId);
        }

        public async Task SubscribeGroup(int groupId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.Add(connectionId, groupId);
            _logger.LogInformation("客户端 {ConnectionId} 订阅点位组 {GroupId}", connectionId, groupId);

            await Clients.Caller.SendAsync("SubscriptionConfirmed", groupId);
        }

        public async Task SubscribePoint(int pointId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.Add(connectionId, pointId);
            _logger.LogInformation("客户端 {ConnectionId} 订阅点位 {PointId}", connectionId, pointId);

            await Clients.Caller.SendAsync("SubscriptionConfirmed", pointId);
        }

        public async Task UnsubscribeDevice(int deviceId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.Remove(connectionId, deviceId);
            _logger.LogInformation("客户端 {ConnectionId} 取消订阅设备 {DeviceId}", connectionId, deviceId);

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", deviceId);
        }

        public async Task UnsubscribeGroup(int groupId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.Remove(connectionId, groupId);
            _logger.LogInformation("客户端 {ConnectionId} 取消订阅点位组 {GroupId}", connectionId, groupId);

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", groupId);
        }

        public async Task UnsubscribePoint(int pointId)
        {
            var connectionId = Context.ConnectionId;
            _subscriptionManager.Remove(connectionId, pointId);
            _logger.LogInformation("客户端 {ConnectionId} 取消订阅点位 {PointId}", connectionId, pointId);

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", pointId);
        }

        public async Task GetSubscriptionStatus()
        {
            var connectionId = Context.ConnectionId;
            var subscriptions = _subscriptionManager.Get(connectionId);

            var status = new
            {
                ConnectionId = connectionId,
                Subscriptions = subscriptions
            };

            await Clients.Caller.SendAsync("SubscriptionStatus", status);
        }

        /************************** 推送数据 **************************/
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

            var subscribedConnections = _subscriptionManager.GetConnectionsByItem(pointId);
            if (subscribedConnections.Any())
            {
                await Clients.Clients(subscribedConnections).SendAsync("ReceivePointUpdate", message);
                _logger.LogDebug("推送点位 {PointId} 更新到 {Count} 个客户端", pointId, subscribedConnections.Count);
            }
        }

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

        public async Task PushPointStatusChange(int pointId, DataPointStatus status)
        {
            var message = new
            {
                Type = "PointStatusChange",
                PointId = pointId,
                Status = status.ToString(),
                UpdateTime = DateTime.UtcNow
            };

            var subscribedConnections = _subscriptionManager.GetConnectionsByItem(pointId);
            if (subscribedConnections.Any())
            {
                await Clients.Clients(subscribedConnections).SendAsync("ReceivePointStatusChange", message);
                _logger.LogDebug("推送点位 {PointId} 状态变更到 {Count} 个客户端", pointId, subscribedConnections.Count);
            }
        }

        public async Task PushPointRemoved(int pointId)
        {
            var message = new
            {
                Type = "PointRemoved",
                PointId = pointId,
                UpdateTime = DateTime.UtcNow
            };

            var subscribedConnections = _subscriptionManager.GetConnectionsByItem(pointId);
            if (subscribedConnections.Any())
            {
                await Clients.Clients(subscribedConnections).SendAsync("ReceivePointRemoved", message);
                _logger.LogDebug("推送点位 {PointId} 移除通知到 {Count} 个客户端", pointId, subscribedConnections.Count);
            }
        }


        public async Task GetConnectionStatistics()
        {
            var allConnections = _subscriptionManager.GetAllConnections();
            var totalSubscriptions = allConnections.Sum(c => _subscriptionManager.Get(c).Count);

            var statistics = new
            {
                TotalConnections = allConnections.Count,
                TotalSubscriptions = totalSubscriptions,
                ConnectionIds = allConnections
            };

            await Clients.Caller.SendAsync("ConnectionStatistics", statistics);
        }
    }
} 