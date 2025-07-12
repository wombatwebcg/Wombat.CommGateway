using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Services.DataCollection;

namespace Wombat.CommGateway.Application.Hubs
{
    /// <summary>
    /// 数据采集实时推送Hub
    /// 负责管理客户端订阅关系，数据推送通过统一的DataDistributionService处理
    /// 支持设备组、设备、点位三种订阅类型
    /// 
    /// 注意：此Hub只负责订阅管理，所有数据推送都通过DataDistributionService统一处理
    /// 这确保了推送逻辑的一致性和基于订阅关系的精确推送
    /// </summary>
    public class DataCollectionHub : Hub
    {
        private readonly ILogger<DataCollectionHub> _logger;
        private readonly ISubscriptionManager _subscriptionManager;

        // 连接用户映射（如需要）
        private static readonly ConcurrentDictionary<string, string> _connectionUsers = new();

        public DataCollectionHub(
            ILogger<DataCollectionHub> logger,
            ISubscriptionManager subscriptionManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        /************************** 连接生命周期 **************************/
        
        /// <summary>
        /// 客户端连接时的处理
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("客户端 {ConnectionId} 已连接", connectionId);

            // 确保连接状态清洁，移除可能存在的旧记录
            _subscriptionManager.RemoveConnection(connectionId);
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 客户端断开连接时的处理
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("客户端 {ConnectionId} 已断开连接", connectionId);

            // 清理订阅关系和连接记录
            _subscriptionManager.RemoveConnection(connectionId);
            _connectionUsers.TryRemove(connectionId, out _);
            
            await base.OnDisconnectedAsync(exception);
        }

        /************************** 订阅管理 **************************/
        
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

        /************************** 查询功能 **************************/
        
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