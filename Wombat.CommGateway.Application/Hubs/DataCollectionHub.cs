using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Hubs
{
    /// <summary>
    /// 数据采集实时推送Hub
    /// 负责将缓存中的点位数据实时推送到前端
    /// </summary>
    public class DataCollectionHub : Hub
    {
        private readonly ILogger<DataCollectionHub> _logger;
        private readonly ICacheUpdateNotificationService _cacheUpdateNotificationService;
        private static readonly ConcurrentDictionary<string, HashSet<int>> _userSubscriptions = new();
        private static readonly ConcurrentDictionary<string, string> _connectionUsers = new();

        public DataCollectionHub(
            ILogger<DataCollectionHub> logger,
            ICacheUpdateNotificationService cacheUpdateNotificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheUpdateNotificationService = cacheUpdateNotificationService ?? throw new ArgumentNullException(nameof(cacheUpdateNotificationService));
        }

        /// <summary>
        /// 客户端连接时
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation($"客户端 {connectionId} 已连接");
            
            // 初始化用户订阅
            _userSubscriptions[connectionId] = new HashSet<int>();
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 客户端断开连接时
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation($"客户端 {connectionId} 已断开连接");

            // 清理用户订阅
            _userSubscriptions.TryRemove(connectionId, out _);
            _connectionUsers.TryRemove(connectionId, out _);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 订阅指定设备的点位数据
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        public async Task SubscribeDevice(int deviceId)
        {
            var connectionId = Context.ConnectionId;
            
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.Add(deviceId);
                _logger.LogInformation($"客户端 {connectionId} 订阅设备 {deviceId}");
            }

            await Clients.Caller.SendAsync("SubscriptionConfirmed", deviceId);
        }

        /// <summary>
        /// 订阅指定点位组的数据
        /// </summary>
        /// <param name="groupId">点位组ID</param>
        public async Task SubscribeGroup(int groupId)
        {
            var connectionId = Context.ConnectionId;
            
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.Add(groupId);
                _logger.LogInformation($"客户端 {connectionId} 订阅点位组 {groupId}");
            }

            await Clients.Caller.SendAsync("SubscriptionConfirmed", groupId);
        }

        /// <summary>
        /// 订阅指定点位的数据
        /// </summary>
        /// <param name="pointId">点位ID</param>
        public async Task SubscribePoint(int pointId)
        {
            var connectionId = Context.ConnectionId;
            
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.Add(pointId);
                _logger.LogInformation($"客户端 {connectionId} 订阅点位 {pointId}");
            }

            await Clients.Caller.SendAsync("SubscriptionConfirmed", pointId);
        }

        /// <summary>
        /// 取消订阅指定设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        public async Task UnsubscribeDevice(int deviceId)
        {
            var connectionId = Context.ConnectionId;
            
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.Remove(deviceId);
                _logger.LogInformation($"客户端 {connectionId} 取消订阅设备 {deviceId}");
            }

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", deviceId);
        }

        /// <summary>
        /// 取消订阅指定点位组
        /// </summary>
        /// <param name="groupId">点位组ID</param>
        public async Task UnsubscribeGroup(int groupId)
        {
            var connectionId = Context.ConnectionId;
            
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.Remove(groupId);
                _logger.LogInformation($"客户端 {connectionId} 取消订阅点位组 {groupId}");
            }

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", groupId);
        }

        /// <summary>
        /// 取消订阅指定点位
        /// </summary>
        /// <param name="pointId">点位ID</param>
        public async Task UnsubscribePoint(int pointId)
        {
            var connectionId = Context.ConnectionId;
            
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                subscriptions.Remove(pointId);
                _logger.LogInformation($"客户端 {connectionId} 取消订阅点位 {pointId}");
            }

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", pointId);
        }

        /// <summary>
        /// 获取当前订阅状态
        /// </summary>
        public async Task GetSubscriptionStatus()
        {
            var connectionId = Context.ConnectionId;
            
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                var status = new
                {
                    ConnectionId = connectionId,
                    Subscriptions = subscriptions.ToList()
                };
                
                await Clients.Caller.SendAsync("SubscriptionStatus", status);
            }
        }

        /// <summary>
        /// 推送单个点位数据更新
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

            // 推送给订阅了该点位的客户端
            var subscribedConnections = GetSubscribedConnections(pointId);
            if (subscribedConnections.Any())
            {
                await Clients.Clients(subscribedConnections).SendAsync("ReceivePointUpdate", message);
                _logger.LogDebug($"推送点位 {pointId} 更新到 {subscribedConnections.Count()} 个客户端");
            }
        }

        /// <summary>
        /// 推送批量点位数据更新
        /// </summary>
        /// <param name="updates">点位更新列表</param>
        public async Task PushBatchPointsUpdate(List<object> updates)
        {
            var message = new
            {
                Type = "BatchPointsUpdate",
                Updates = updates,
                UpdateTime = DateTime.UtcNow
            };

            // 推送给所有连接的客户端
            await Clients.All.SendAsync("ReceiveBatchPointsUpdate", message);
            _logger.LogDebug($"推送批量点位更新到所有客户端，共 {updates.Count} 个点位");
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

            // 推送给订阅了该点位的客户端
            var subscribedConnections = GetSubscribedConnections(pointId);
            if (subscribedConnections.Any())
            {
                await Clients.Clients(subscribedConnections).SendAsync("ReceivePointStatusChange", message);
                _logger.LogDebug($"推送点位 {pointId} 状态变更到 {subscribedConnections.Count()} 个客户端");
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

            // 推送给订阅了该点位的客户端
            var subscribedConnections = GetSubscribedConnections(pointId);
            if (subscribedConnections.Any())
            {
                await Clients.Clients(subscribedConnections).SendAsync("ReceivePointRemoved", message);
                _logger.LogDebug($"推送点位 {pointId} 移除通知到 {subscribedConnections.Count()} 个客户端");
            }
        }

        /// <summary>
        /// 获取订阅了指定点位的连接ID列表
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>连接ID列表</returns>
        private IEnumerable<string> GetSubscribedConnections(int pointId)
        {
            var subscribedConnections = new List<string>();
            
            foreach (var kvp in _userSubscriptions)
            {
                if (kvp.Value.Contains(pointId))
                {
                    subscribedConnections.Add(kvp.Key);
                }
            }
            
            return subscribedConnections;
        }

        /// <summary>
        /// 获取订阅了指定设备的连接ID列表
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>连接ID列表</returns>
        private IEnumerable<string> GetDeviceSubscribedConnections(int deviceId)
        {
            var subscribedConnections = new List<string>();
            
            foreach (var kvp in _userSubscriptions)
            {
                if (kvp.Value.Contains(deviceId))
                {
                    subscribedConnections.Add(kvp.Key);
                }
            }
            
            return subscribedConnections;
        }

        /// <summary>
        /// 获取当前连接统计信息
        /// </summary>
        public async Task GetConnectionStatistics()
        {
            var statistics = new
            {
                TotalConnections = _userSubscriptions.Count,
                TotalSubscriptions = _userSubscriptions.Values.Sum(s => s.Count),
                ConnectionIds = _userSubscriptions.Keys.ToList()
            };
            
            await Clients.Caller.SendAsync("ConnectionStatistics", statistics);
        }
    }
} 