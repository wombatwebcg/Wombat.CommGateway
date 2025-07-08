using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Application.Services.DataCollection;
using Wombat.CommGateway.Application.Services.DataCollection.Models;
using Wombat.Extensions.AutoGenerator.Attributes;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// WebSocket数据采集服务
    /// 提供与SignalR Hub一致的API，支持设备、点位组、点位的订阅管理
    /// </summary>
    /// 

    [AutoInject(ServiceLifetime = ServiceLifetime.Singleton)]

    public class WebSocketService
    {
        private readonly ILogger<WebSocketService> _logger;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IDataPushBus _dataPushBus;
        private readonly CacheManager _cacheManager;
        
        // 连接管理
        private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
        private readonly ConcurrentDictionary<string, DateTime> _connectionTimestamps = new();
        
        // 消息序列化选项
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public WebSocketService(
            ILogger<WebSocketService> logger,
            ISubscriptionManager subscriptionManager,
            IDataPushBus dataPushBus,
            CacheManager cacheManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _dataPushBus = dataPushBus ?? throw new ArgumentNullException(nameof(dataPushBus));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        }

        /// <summary>
        /// 处理WebSocket连接
        /// </summary>
        public async Task HandleConnectionAsync(WebSocket webSocket, string connectionId)
        {
            try
            {
                // 注册连接
                _connections.TryAdd(connectionId, webSocket);
                _connectionTimestamps.TryAdd(connectionId, DateTime.UtcNow);
                
                _logger.LogInformation("WebSocket连接已建立 - 连接ID: {ConnectionId}", connectionId);

                // 发送连接确认消息
                var connectionMessage = WebSocketMessage.CreateConnectionStatus(connectionId, new
                {
                    connectionId,
                    status = "connected",
                    timestamp = DateTime.UtcNow
                });
                await SendToConnectionAsync(connectionId, connectionMessage);

                // 注册数据推送监听器
                await using var pushBusRegistration = _dataPushBus.RegisterAsync(async msg =>
                {
                    await HandleDataPushMessageAsync(connectionId, msg);
                });

                // 处理客户端消息
                await HandleClientMessagesAsync(webSocket, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理WebSocket连接时发生错误 - 连接ID: {ConnectionId}", connectionId);
            }
            finally
            {
                // 清理连接
                await CleanupConnectionAsync(connectionId);
            }
        }

        /// <summary>
        /// 处理客户端消息
        /// </summary>
        private async Task HandleClientMessagesAsync(WebSocket webSocket, string connectionId)
        {
            var buffer = new byte[4 * 1024];
            
            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("客户端主动关闭连接 - 连接ID: {ConnectionId}", connectionId);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessClientCommandAsync(connectionId, json);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理客户端消息时发生错误 - 连接ID: {ConnectionId}", connectionId);
                    break;
                }
            }
        }

        /// <summary>
        /// 处理客户端命令
        /// </summary>
        private async Task ProcessClientCommandAsync(string connectionId, string json)
        {
            try
            {
                var command = JsonSerializer.Deserialize<WebSocketCommand>(json, _jsonOptions);
                
                if (command == null || !command.IsValid())
                {
                    var errorMessage = WebSocketMessage.CreateError("无效的命令格式", connectionId);
                    await SendToConnectionAsync(connectionId, errorMessage);
                    return;
                }

                _logger.LogDebug("收到客户端命令 - 连接ID: {ConnectionId}, 命令: {Command}", 
                    connectionId, command.GetDescription());

                switch (command.Action)
                {
                    case CommandActions.Subscribe:
                        await HandleSubscribeAsync(connectionId, command);
                        break;
                        
                    case CommandActions.Unsubscribe:
                        await HandleUnsubscribeAsync(connectionId, command);
                        break;
                        
                    case CommandActions.Ping:
                        await HandlePingAsync(connectionId);
                        break;
                        
                    case CommandActions.GetSubscriptionStatus:
                        await HandleGetSubscriptionStatusAsync(connectionId);
                        break;
                        
                    case CommandActions.GetConnectionStatistics:
                        await HandleGetConnectionStatisticsAsync(connectionId);
                        break;
                        
                    default:
                        var errorMessage = WebSocketMessage.CreateError($"未知命令: {command.Action}", connectionId);
                        await SendToConnectionAsync(connectionId, errorMessage);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析客户端命令JSON时发生错误 - 连接ID: {ConnectionId}", connectionId);
                var errorMessage = WebSocketMessage.CreateError("JSON格式错误", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理客户端命令时发生错误 - 连接ID: {ConnectionId}", connectionId);
                var errorMessage = WebSocketMessage.CreateError("处理命令时发生错误", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
        }

        /// <summary>
        /// 处理订阅命令
        /// </summary>
        private async Task HandleSubscribeAsync(string connectionId, WebSocketCommand command)
        {
            if (command.Target == SubscriptionTargets.Device)
            {
                // 从CacheManager获取设备下所有点位ID
                var pointIds = GetPointIdsByDeviceFromCache(command.Id);
                foreach (var pointId in pointIds)
                {
                    _subscriptionManager.Add(connectionId, pointId);
                }
                _logger.LogInformation("客户端 {ConnectionId} 订阅设备 {Id}，共{Count}个点位(来自缓存)", connectionId, command.Id, pointIds.Count);
            }
            else if (command.Target == SubscriptionTargets.Group)
            {
                // 从CacheManager获取组下所有点位ID
                var pointIds = GetPointIdsByGroupFromCache(command.Id);
                foreach (var pointId in pointIds)
                {
                    _subscriptionManager.Add(connectionId, pointId);
                }
                _logger.LogInformation("客户端 {ConnectionId} 订阅组 {Id}，共{Count}个点位(来自缓存)", connectionId, command.Id, pointIds.Count);
            }
            else if (command.Target == SubscriptionTargets.Point)
            {
                _subscriptionManager.Add(connectionId, command.Id);
                _logger.LogInformation("客户端 {ConnectionId} 订阅点位 {Id}", connectionId, command.Id);
            }
            else
            {
                var errorMessage = WebSocketMessage.CreateError("未知订阅目标类型", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
                return;
            }
            var confirmMessage = WebSocketMessage.CreateSubscriptionConfirmed(command.Target!, command.Id);
            await SendToConnectionAsync(connectionId, confirmMessage);
        }

        /// <summary>
        /// 处理取消订阅命令
        /// </summary>
        private async Task HandleUnsubscribeAsync(string connectionId, WebSocketCommand command)
        {
            if (command.Target == SubscriptionTargets.Device)
            {
                var pointIds = GetPointIdsByDeviceFromCache(command.Id);
                foreach (var pointId in pointIds)
                {
                    _subscriptionManager.Remove(connectionId, pointId);
                }
                _logger.LogInformation("客户端 {ConnectionId} 取消订阅设备 {Id}，共{Count}个点位(来自缓存)", connectionId, command.Id, pointIds.Count);
            }
            else if (command.Target == SubscriptionTargets.Group)
            {
                var pointIds = GetPointIdsByGroupFromCache(command.Id);
                foreach (var pointId in pointIds)
                {
                    _subscriptionManager.Remove(connectionId, pointId);
                }
                _logger.LogInformation("客户端 {ConnectionId} 取消订阅组 {Id}，共{Count}个点位(来自缓存)", connectionId, command.Id, pointIds.Count);
            }
            else if (command.Target == SubscriptionTargets.Point)
            {
                _subscriptionManager.Remove(connectionId, command.Id);
                _logger.LogInformation("客户端 {ConnectionId} 取消订阅点位 {Id}", connectionId, command.Id);
            }
            else
            {
                var errorMessage = WebSocketMessage.CreateError("未知取消订阅目标类型", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
                return;
            }
            var confirmMessage = WebSocketMessage.CreateUnsubscriptionConfirmed(command.Target!, command.Id);
            await SendToConnectionAsync(connectionId, confirmMessage);
        }

        /// <summary>
        /// 处理心跳命令
        /// </summary>
        private async Task HandlePingAsync(string connectionId)
        {
            var pongMessage = WebSocketMessage.CreatePong();
            await SendToConnectionAsync(connectionId, pongMessage);
        }

        /// <summary>
        /// 处理获取订阅状态命令
        /// </summary>
        private async Task HandleGetSubscriptionStatusAsync(string connectionId)
        {
            var subscriptions = _subscriptionManager.Get(connectionId);
            var statusMessage = WebSocketMessage.CreateSubscriptionStatus(connectionId, new
            {
                connectionId,
                subscriptions = subscriptions
            });
            await SendToConnectionAsync(connectionId, statusMessage);
        }

        /// <summary>
        /// 处理获取连接统计命令
        /// </summary>
        private async Task HandleGetConnectionStatisticsAsync(string connectionId)
        {
            var allConnections = _subscriptionManager.GetAllConnections();
            var totalSubscriptions = allConnections.Sum(c => _subscriptionManager.Get(c).Count);
            
            var statisticsMessage = new WebSocketMessage
            {
                Type = "connection_statistics",
                Data = new
                {
                    totalConnections = allConnections.Count,
                    totalSubscriptions = totalSubscriptions,
                    connectionIds = allConnections
                },
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };
            await SendToConnectionAsync(connectionId, statisticsMessage);
        }

        /// <summary>
        /// 处理数据推送消息
        /// </summary>
        private async Task HandleDataPushMessageAsync(string connectionId, object message)
        {
            try
            {
                // 检查连接是否仍然存在
                if (!_connections.ContainsKey(connectionId))
                {
                    return;
                }

                // 根据消息类型确定是否需要推送给该连接
                if (message is JsonElement element && element.TryGetProperty("Type", out var typeElement))
                {
                    var messageType = typeElement.GetString();
                    var shouldPush = messageType switch
                    {
                        "PointUpdate" => ShouldPushPointUpdate(connectionId, element),
                        "BatchPointsUpdate" => true, // 批量更新推送给所有连接
                        "PointStatusChange" => ShouldPushPointUpdate(connectionId, element),
                        "PointRemoved" => ShouldPushPointUpdate(connectionId, element),
                        _ => false
                    };

                    if (shouldPush)
                    {
                        var wsMessage = new WebSocketMessage
                        {
                            Type = messageType.ToLowerInvariant(),
                            Data = message,
                            Timestamp = DateTime.UtcNow
                        };
                        await SendToConnectionAsync(connectionId, wsMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理数据推送消息时发生错误 - 连接ID: {ConnectionId}", connectionId);
            }
        }

        /// <summary>
        /// 检查是否应该推送点位更新给指定连接
        /// </summary>
        private bool ShouldPushPointUpdate(string connectionId, JsonElement messageElement)
        {
            try
            {
                if (messageElement.TryGetProperty("PointId", out var pointIdElement))
                {
                    var pointId = pointIdElement.GetInt32();
                    var subscribedConnections = _subscriptionManager.GetConnectionsByItem(pointId);
                    return subscribedConnections.Contains(connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查点位更新推送条件时发生错误");
            }
            return false;
        }

        /// <summary>
        /// 发送消息到指定连接
        /// </summary>
        public async Task SendToConnectionAsync(string connectionId, WebSocketMessage message)
        {
            if (_connections.TryGetValue(connectionId, out var webSocket) && 
                webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var json = JsonSerializer.Serialize(message, _jsonOptions);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    await webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送消息到连接时发生错误 - 连接ID: {ConnectionId}", connectionId);
                    await CleanupConnectionAsync(connectionId);
                }
            }
        }

        /// <summary>
        /// 广播消息到多个连接
        /// </summary>
        public async Task BroadcastAsync(WebSocketMessage message, IEnumerable<string> connectionIds)
        {
            var tasks = connectionIds.Select(id => SendToConnectionAsync(id, message));
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 清理连接
        /// </summary>
        private async Task CleanupConnectionAsync(string connectionId)
        {
            try
            {
                // 移除订阅
                _subscriptionManager.RemoveConnection(connectionId);
                
                // 移除连接记录
                _connections.TryRemove(connectionId, out _);
                _connectionTimestamps.TryRemove(connectionId, out _);
                
                _logger.LogInformation("WebSocket连接已清理 - 连接ID: {ConnectionId}", connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理连接时发生错误 - 连接ID: {ConnectionId}", connectionId);
            }
        }

        /// <summary>
        /// 获取当前连接数
        /// </summary>
        public int GetConnectionCount() => _connections.Count;

        /// <summary>
        /// 获取所有连接ID
        /// </summary>
        public IReadOnlyList<string> GetAllConnectionIds() => _connections.Keys.ToList();

        /// <summary>
        /// 从CacheManager获取设备下所有点位ID
        /// </summary>
        private List<int> GetPointIdsByDeviceFromCache(int deviceId)
        {
            // 假设CacheManager有GetAllCachedValues或类似方法，返回所有点位及其DeviceId
            // 这里需根据实际CacheManager实现调整
            var all = _cacheManager.BatchGetCachedValues(_cacheManager.GetAllDirtyData().Keys);
            return all.Keys
                .Select(pointId => new { pointId, deviceId = TryGetDeviceId(pointId) })
                .Where(x => x.deviceId == deviceId)
                .Select(x => x.pointId)
                .ToList();
        }

        /// <summary>
        /// 从CacheManager获取组下所有点位ID
        /// </summary>
        private List<int> GetPointIdsByGroupFromCache(int groupId)
        {
            // 假设CacheManager有GetAllCachedValues或类似方法，返回所有点位及其GroupId
            // 这里需根据实际CacheManager实现调整
            var all = _cacheManager.BatchGetCachedValues(_cacheManager.GetAllDirtyData().Keys);
            return all.Keys
                .Select(pointId => new { pointId, groupId = TryGetGroupId(pointId) })
                .Where(x => x.groupId == groupId)
                .Select(x => x.pointId)
                .ToList();
        }

        // 需要实现TryGetDeviceId和TryGetGroupId方法，通常需要点位元数据支持
        private int TryGetDeviceId(int pointId)
        {
            // TODO: 实现通过pointId查找DeviceId的逻辑
            // 可通过IDevicePointService或缓存的点位元数据实现
            return 0;
        }

        private int TryGetGroupId(int pointId)
        {
            // TODO: 实现通过pointId查找GroupId的逻辑
            // 可通过IDevicePointService或缓存的点位元数据实现
            return 0;
        }
    }
} 