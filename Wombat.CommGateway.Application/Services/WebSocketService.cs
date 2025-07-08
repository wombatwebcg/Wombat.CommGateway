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
using Wombat.CommGateway.Domain.Repositories;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// WebSocket数据采集服务
    /// 提供与SignalR Hub一致的API，支持设备组、设备、点位三种订阅管理
    /// </summary>
    /// 

    [AutoInject(ServiceLifetime = ServiceLifetime.Singleton)]

    public class WebSocketService : IAsyncDisposable
    {
        private readonly ILogger<WebSocketService> _logger;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IDataPushBus _dataPushBus;
        private readonly CacheManager _cacheManager;
        IServiceScopeFactory _serviceScopeFactory;
        
        // 连接管理
        private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
        private readonly ConcurrentDictionary<string, DateTime> _connectionTimestamps = new();
        
        // 全局数据推送监听器
        private IAsyncDisposable? _globalPushBusRegistration;
        private volatile bool _isInitialized = false;
        private readonly object _initLock = new object();
        
        // 消息序列化选项
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public WebSocketService(
            ILogger<WebSocketService> logger,
            ISubscriptionManager subscriptionManager,
            IServiceScopeFactory serviceScopeFactory,
            IDataPushBus dataPushBus,
            CacheManager cacheManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _dataPushBus = dataPushBus ?? throw new ArgumentNullException(nameof(dataPushBus));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        /// <summary>
        /// 初始化全局数据推送监听器
        /// </summary>
        private async Task InitializeGlobalPushListenerAsync()
        {
            if (_isInitialized)
                return;

            lock (_initLock)
            {
                if (_isInitialized)
                    return;

                _logger.LogInformation("初始化WebSocket全局数据推送监听器...");
                
                // 注册全局数据推送监听器
                _globalPushBusRegistration = _dataPushBus.RegisterAsync(async msg =>
                {
                    await HandleGlobalDataPushMessageAsync(msg);
                });

                _isInitialized = true;
                _logger.LogInformation("WebSocket全局数据推送监听器已初始化");
            }
        }

        /// <summary>
        /// 处理WebSocket连接
        /// </summary>
        public async Task HandleConnectionAsync(WebSocket webSocket, string connectionId)
        {
            try
            {
                // 确保全局推送监听器已初始化
                await InitializeGlobalPushListenerAsync();

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
        /// 使用新的类型明确的订阅API
        /// </summary>
        private async Task HandleSubscribeAsync(string connectionId, WebSocketCommand command)
        {
            if (command.Target == SubscriptionTargets.Device)
            {
                _subscriptionManager.SubscribeToDevice(connectionId, command.Id);
                _logger.LogInformation("客户端 {ConnectionId} 订阅设备 {Id}", connectionId, command.Id);
            }
            else if (command.Target == SubscriptionTargets.Group)
            {
                _subscriptionManager.SubscribeToGroup(connectionId, command.Id);
                _logger.LogInformation("客户端 {ConnectionId} 订阅设备组 {Id}", connectionId, command.Id);
            }
            else if (command.Target == SubscriptionTargets.Point)
            {
                _subscriptionManager.SubscribeToPoint(connectionId, command.Id);
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
        /// 使用新的类型明确的取消订阅API
        /// </summary>
        private async Task HandleUnsubscribeAsync(string connectionId, WebSocketCommand command)
        {
            if (command.Target == SubscriptionTargets.Device)
            {
                _subscriptionManager.UnsubscribeFromDevice(connectionId, command.Id);
                _logger.LogInformation("客户端 {ConnectionId} 取消订阅设备 {Id}", connectionId, command.Id);
            }
            else if (command.Target == SubscriptionTargets.Group)
            {
                _subscriptionManager.UnsubscribeFromGroup(connectionId, command.Id);
                _logger.LogInformation("客户端 {ConnectionId} 取消订阅设备组 {Id}", connectionId, command.Id);
            }
            else if (command.Target == SubscriptionTargets.Point)
            {
                _subscriptionManager.UnsubscribeFromPoint(connectionId, command.Id);
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
        /// 使用新的GetConnectionStatus API
        /// </summary>
        private async Task HandleGetSubscriptionStatusAsync(string connectionId)
        {
            var status = _subscriptionManager.GetConnectionStatus(connectionId);
            var statusMessage = WebSocketMessage.CreateSubscriptionStatus(connectionId, new
            {
                connectionId = status.ConnectionId,
                totalSubscriptions = status.TotalSubscriptions,
                groupSubscriptions = status.GroupSubscriptions,
                deviceSubscriptions = status.DeviceSubscriptions,
                pointSubscriptions = status.PointSubscriptions,
                lastActivityTime = status.LastActivityTime
            });
            await SendToConnectionAsync(connectionId, statusMessage);
        }

        /// <summary>
        /// 处理获取连接统计命令
        /// 使用新的GetConnectionStatus API获取详细统计
        /// </summary>
        private async Task HandleGetConnectionStatisticsAsync(string connectionId)
        {
            var allConnections = _subscriptionManager.GetAllConnections();
            var totalSubscriptions = 0;
            var groupSubscriptions = 0;
            var deviceSubscriptions = 0;
            var pointSubscriptions = 0;

            // 统计各类订阅数量
            foreach (var connId in allConnections)
            {
                var status = _subscriptionManager.GetConnectionStatus(connId);
                groupSubscriptions += status.GroupSubscriptions.Count;
                deviceSubscriptions += status.DeviceSubscriptions.Count;
                pointSubscriptions += status.PointSubscriptions.Count;
                totalSubscriptions += status.TotalSubscriptions;
            }
            
            var statisticsMessage = new WebSocketMessage
            {
                Type = "connection_statistics",
                Data = new
                {
                    totalConnections = allConnections.Count,
                    totalSubscriptions = totalSubscriptions,
                    groupSubscriptions = groupSubscriptions,
                    deviceSubscriptions = deviceSubscriptions,
                    pointSubscriptions = pointSubscriptions,
                    connectionIds = allConnections
                },
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };
            await SendToConnectionAsync(connectionId, statisticsMessage);
        }

        /// <summary>
        /// 处理全局数据推送消息
        /// </summary>
        private async Task HandleGlobalDataPushMessageAsync(object message)
        {
            try
            {
                if (message is JsonElement element && element.TryGetProperty("Type", out var typeElement))
                {
                    var messageType = typeElement.GetString();
                    
                    switch (messageType)
                    {
                        case "PointUpdate":
                            await HandlePointUpdateMessage(element);
                            break;
                        case "BatchPointsUpdate":
                            await HandleBatchPointsUpdateMessage(element);
                            break;
                        case "PointStatusChange":
                            await HandlePointUpdateMessage(element); // 使用相同的逻辑
                            break;
                        case "PointRemoved":
                            await HandlePointUpdateMessage(element); // 使用相同的逻辑
                            break;
                        default:
                            _logger.LogDebug("收到未知类型的数据推送消息: {MessageType}", messageType);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理全局数据推送消息时发生错误");
            }
        }

        /// <summary>
        /// 处理点位更新消息
        /// 使用优化的GetConnectionsForPointUpdate方法获取所有相关连接
        /// </summary>
        private async Task HandlePointUpdateMessage(JsonElement messageElement)
        {
            try
            {
                if (messageElement.TryGetProperty("PointId", out var pointIdElement))
                {
                    var pointId = pointIdElement.GetInt32();
                    
                    // 使用优化的数据推送查询，一次性获取所有相关连接（包括层级订阅）
                    var subscribedConnections = _subscriptionManager.GetConnectionsForPointUpdate(pointId);
                    
                    if (subscribedConnections.Any())
                    {
                        var messageType = messageElement.TryGetProperty("Type", out var typeElement) 
                            ? typeElement.GetString()?.ToLowerInvariant() 
                            : "point_update";

                        var wsMessage = new WebSocketMessage
                        {
                            Type = messageType,
                            Data = JsonDocument.Parse(messageElement.GetRawText()).RootElement,
                            Timestamp = DateTime.UtcNow
                        };

                        // 并发推送给所有订阅的连接
                        var pushTasks = subscribedConnections.Select(connectionId => 
                            SendToConnectionAsync(connectionId, wsMessage));
                        
                        await Task.WhenAll(pushTasks);
                        
                        _logger.LogDebug("点位 {PointId} 更新已推送到 {Count} 个连接（包括层级订阅）", pointId, subscribedConnections.Count);
                    }
                    else
                    {
                        _logger.LogDebug("点位 {PointId} 更新没有找到订阅的连接", pointId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理点位更新消息时发生错误");
            }
        }

        /// <summary>
        /// 处理批量点位更新消息
        /// </summary>
        private async Task HandleBatchPointsUpdateMessage(JsonElement messageElement)
        {
            try
            {
                // 批量更新消息推送给所有连接
                var allConnections = _subscriptionManager.GetAllConnections();
                
                if (allConnections.Any())
                {
                    var wsMessage = new WebSocketMessage
                    {
                        Type = "batch_points_update",
                        Data = JsonDocument.Parse(messageElement.GetRawText()).RootElement,
                        Timestamp = DateTime.UtcNow
                    };

                    // 并发推送给所有连接
                    var pushTasks = allConnections.Select(connectionId => 
                        SendToConnectionAsync(connectionId, wsMessage));
                    
                    await Task.WhenAll(pushTasks);
                    
                    _logger.LogDebug("批量点位更新已推送到 {Count} 个连接", allConnections.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理批量点位更新消息时发生错误");
            }
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
        /// 释放资源
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_globalPushBusRegistration != null)
            {
                await _globalPushBusRegistration.DisposeAsync();
            }
        }
    }
} 