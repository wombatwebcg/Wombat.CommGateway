using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
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

    [AutoInject<WebSocketService>(ServiceLifetime = ServiceLifetime.Singleton)]
    public class WebSocketService : IAsyncDisposable
    {
        private readonly ILogger<WebSocketService> _logger;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IDataPushBus _dataPushBus;
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
            IDataPushBus dataPushBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _dataPushBus = dataPushBus ?? throw new ArgumentNullException(nameof(dataPushBus));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            
            // 立即初始化全局数据推送监听器，确保能接收到DataPushBus的消息
            _ = Task.Run(async () => await InitializeGlobalPushListenerAsync());
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
                // 全局推送监听器已在构造函数中初始化，无需重复调用

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
                        
                    case CommandActions.GetSchedulerStatus:
                        await HandleGetSchedulerStatusAsync(connectionId);
                        break;
                        
                    case CommandActions.GetHierarchyStatus:
                        await HandleGetHierarchyStatusAsync(connectionId, command.Id);
                        break;
                        
                    case CommandActions.TestDataPush:
                        await HandleTestDataPushAsync(connectionId);
                        break;
                        
                    case CommandActions.GetPointStatus:
                        await HandleGetPointStatusAsync(connectionId, command.Id);
                        break;
                        
                    case CommandActions.GetPointSubscriptionDetails:
                        await HandleGetPointSubscriptionDetailsAsync(connectionId, command.Id);
                        break;
                        
                    case CommandActions.GetDeviceData:
                        await HandleGetDeviceDataAsync(connectionId, command.Id);
                        break;
                        
                    case CommandActions.GetGroupData:
                        await HandleGetGroupDataAsync(connectionId, command.Id);
                        break;
                        
                    case CommandActions.GetAllConnectionDetails:
                        await HandleGetAllConnectionDetailsAsync(connectionId);
                        break;
                        
                    case CommandActions.ForceCleanupSubscription:
                        await HandleForceCleanupSubscriptionAsync(connectionId, command);
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
            var confirmMessage = WebSocketMessage.CreateSubscriptionConfirmed(command.Target!, command.Id, connectionId);
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
            var confirmMessage = WebSocketMessage.CreateUnsubscriptionConfirmed(command.Target!, command.Id, connectionId);
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
            var statusMessage = WebSocketMessage.CreateConnectionStatus(connectionId, new
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
                _logger.LogDebug("收到全局数据推送消息: {MessageType}, 内容: {Message}", 
                    message?.GetType().Name, message?.ToString());

                string? messageType = null;
                
                // 尝试从JsonElement获取Type属性
                if (message is JsonElement element && element.TryGetProperty("Type", out var typeElement))
                {
                    messageType = typeElement.GetString();
                    _logger.LogDebug("从JsonElement获取消息类型: {MessageType}", messageType);
                }
                // 尝试从普通对象获取Type属性（使用反射）
                else if (message != null)
                {
                    var messageTypeProperty = message.GetType().GetProperty("Type");
                    if (messageTypeProperty != null)
                    {
                        messageType = messageTypeProperty.GetValue(message)?.ToString();
                        _logger.LogDebug("从对象属性获取消息类型: {MessageType}", messageType);
                    }
                }

                if (string.IsNullOrEmpty(messageType))
                {
                    _logger.LogWarning("无法从消息中获取Type属性，消息类型: {MessageType}", message?.GetType().Name);
                    return;
                }
                
                switch (messageType)
                {
                    case "PointUpdate":
                        await HandlePointUpdateMessage(message);
                        break;
                    case "BatchPointsUpdate":
                        await HandleBatchPointsUpdateMessage(message);
                        break;
                    case "PointStatusChange":
                        await HandlePointUpdateMessage(message); // 使用相同的逻辑
                        break;
                    case "PointRemoved":
                        await HandlePointUpdateMessage(message); // 使用相同的逻辑
                        break;
                    default:
                        _logger.LogDebug("收到未知类型的数据推送消息: {MessageType}", messageType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理全局数据推送消息时发生错误，消息类型: {MessageType}", message?.GetType().Name);
            }
        }

        /// <summary>
        /// 处理点位更新消息
        /// 使用优化的GetConnectionsForPointUpdate方法获取所有相关连接
        /// </summary>
        private async Task HandlePointUpdateMessage(object message)
        {
            try
            {
                int? pointId = null;
                JsonElement? messageElement = null;
                
                // 尝试从JsonElement获取PointId
                if (message is JsonElement element)
                {
                    messageElement = element;
                    if (element.TryGetProperty("PointId", out var pointIdElement))
                    {
                        pointId = pointIdElement.GetInt32();
                    }
                }
                // 尝试从普通对象获取PointId（使用反射）
                else if (message != null)
                {
                    var pointIdProperty = message.GetType().GetProperty("PointId");
                    if (pointIdProperty != null)
                    {
                        pointId = (int?)pointIdProperty.GetValue(message);
                    }
                }

                if (!pointId.HasValue)
                {
                    _logger.LogWarning("无法从点位更新消息中获取PointId");
                    return;
                }
                
                _logger.LogDebug("处理点位 {PointId} 更新消息", pointId.Value);
                
                // 使用优化的数据推送查询，一次性获取所有相关连接（包括层级订阅）
                var subscribedConnections = _subscriptionManager.GetConnectionsForPointUpdate(pointId.Value);
                
                if (subscribedConnections.Any())
                {
                    var messageType = GetMessageType(message)?.ToLowerInvariant() ?? "point_update";

                    // 为每个连接单独创建消息，确保设置ConnectionId
                    foreach (var connectionId in subscribedConnections)
                    {
                        var wsMessage = new WebSocketMessage
                        {
                            Type = messageType,
                            Data = ConvertToJsonElement(message),
                            ConnectionId = connectionId,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        await SendToConnectionAsync(connectionId, wsMessage);
                    }
                    
                    _logger.LogDebug("点位 {PointId} 更新已推送到 {Count} 个连接（包括层级订阅）", pointId.Value, subscribedConnections.Count);
                }
                else
                {
                    _logger.LogDebug("点位 {PointId} 更新没有找到订阅的连接", pointId.Value);
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
        private async Task HandleBatchPointsUpdateMessage(object message)
        {
            try
            {
                _logger.LogDebug("处理批量点位更新消息");
                
                // 批量更新消息推送给所有连接
                var allConnections = _subscriptionManager.GetAllConnections();
                
                if (allConnections.Any())
                {
                    // 为每个连接单独创建消息，确保设置ConnectionId
                    foreach (var connectionId in allConnections)
                    {
                        var wsMessage = new WebSocketMessage
                        {
                            Type = "batch_points_update",
                            Data = ConvertToJsonElement(message),
                            ConnectionId = connectionId,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        await SendToConnectionAsync(connectionId, wsMessage);
                    }
                    
                    _logger.LogDebug("批量点位更新已推送到 {Count} 个连接", allConnections.Count);
                }
                else
                {
                    _logger.LogDebug("批量点位更新没有找到任何连接");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理批量点位更新消息时发生错误");
            }
        }

        /// <summary>
        /// 从消息对象获取Type属性
        /// </summary>
        private string? GetMessageType(object message)
        {
            if (message is JsonElement element && element.TryGetProperty("Type", out var typeElement))
            {
                return typeElement.GetString();
            }
            else if (message != null)
            {
                var typeProperty = message.GetType().GetProperty("Type");
                return typeProperty?.GetValue(message)?.ToString();
            }
            return null;
        }

        /// <summary>
        /// 将消息对象转换为JsonElement
        /// </summary>
        private object ConvertToJsonElement(object message)
        {
            if (message is JsonElement)
            {
                return message;
            }
            else
            {
                // 将对象序列化为JSON字符串，然后解析为JsonElement
                var json = JsonSerializer.Serialize(message, _jsonOptions);
                return JsonDocument.Parse(json).RootElement;
            }
        }

        /// <summary>
        /// 处理获取调度器状态命令
        /// </summary>
        private async Task HandleGetSchedulerStatusAsync(string connectionId)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dataCollectionService = scope.ServiceProvider.GetService<IDataCollectionService>();
                    var timeWheelScheduler = scope.ServiceProvider.GetService<TimeWheelScheduler>();
                    
                    if (timeWheelScheduler != null)
                    {
                        var statistics = timeWheelScheduler.GetStatistics();
                        var scanRateGroups = timeWheelScheduler.GetScanRateGroupStatistics();
                        var channelGroups = timeWheelScheduler.GetChannelGroupStatistics();
                        
                        // 计算任务频率估算
                        var estimatedTasksPerMinute = 0.0;
                        foreach (var group in scanRateGroups)
                        {
                            var scanRateMs = group.Key;
                            var pointCount = group.Value;
                            if (scanRateMs > 0)
                            {
                                var tasksPerMinute = (60.0 * 1000.0) / scanRateMs; // 每分钟的任务数
                                estimatedTasksPerMinute += tasksPerMinute;
                            }
                        }
                        
                        var schedulerStatusMessage = new WebSocketMessage
                        {
                            Type = "scheduler_status",
                            Data = new
                            {
                                isRunning = timeWheelScheduler.IsRunning,
                                scheduledPointCount = timeWheelScheduler.ScheduledPointCount,
                                statistics = new
                                {
                                    totalScheduledTasks = statistics.TotalScheduledTasks,
                                    successfulTasks = statistics.SuccessfulTasks,
                                    failedTasks = statistics.FailedTasks,
                                    lastExecutionTime = statistics.LastExecutionTime,
                                    successRate = statistics.SuccessRate
                                },
                                scanRateGroups = scanRateGroups.Select(kvp => new 
                                {
                                    scanRateMs = kvp.Key,
                                    pointCount = kvp.Value,
                                    tasksPerMinute = kvp.Key > 0 ? Math.Round((60.0 * 1000.0) / kvp.Key, 2) : 0
                                }).ToList(),
                                channelGroups = channelGroups.Select(kvp => new 
                                {
                                    channelId = kvp.Key,
                                    pointCount = kvp.Value
                                }).ToList(),
                                estimatedTasksPerMinute = Math.Round(estimatedTasksPerMinute, 2),
                                explanation = new
                                {
                                    totalScheduledTasks = "累计创建的采集任务总数（包括已完成的任务）",
                                    whyIncreasing = "每次按扫描周期触发数据采集时都会创建新任务，所以数字会持续增长",
                                    currentRate = $"当前预计每分钟产生约 {Math.Round(estimatedTasksPerMinute, 1)} 个采集任务",
                                    taskGrouping = "点位按(扫描周期,通道ID)分组执行，相同组合的点位会在一个任务中批量采集"
                                }
                            },
                            ConnectionId = connectionId,
                            Timestamp = DateTime.UtcNow
                        };
                        await SendToConnectionAsync(connectionId, schedulerStatusMessage);
                    }
                    else
                    {
                        var errorMessage = WebSocketMessage.CreateError("无法获取调度器实例", connectionId);
                        await SendToConnectionAsync(connectionId, errorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取调度器状态时发生错误 - 连接ID: {ConnectionId}", connectionId);
                var errorMessage = WebSocketMessage.CreateError("获取调度器状态失败", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
        }

        /// <summary>
        /// 处理获取层级关系状态命令
        /// </summary>
        private async Task HandleGetHierarchyStatusAsync(string connectionId, int pointId)
        {
            try
            {
                var connections = _subscriptionManager.GetConnectionsForPointUpdate(pointId);
                var pointSubscribers = _subscriptionManager.GetPointSubscribers(pointId);
                
                // 尝试获取点位所属设备和设备组信息
                string deviceInfo = "未知";
                string groupInfo = "未知";
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var devicePointRepository = scope.ServiceProvider.GetService<Domain.Repositories.IDevicePointRepository>();
                    var deviceRepository = scope.ServiceProvider.GetService<Domain.Repositories.IDeviceRepository>();
                    
                    if (devicePointRepository != null && deviceRepository != null)
                    {
                        var point = await devicePointRepository.GetDevicePointAsync(pointId);
                        if (point != null)
                        {
                            var device = await deviceRepository.GetByIdAsync(point.DeviceId);
                            if (device != null)
                            {
                                deviceInfo = $"设备ID: {device.Id}, 名称: {device.Name}, 启用: {device.Enable}";
                                groupInfo = $"设备组ID: {device.DeviceGroupId}";
                            }
                        }
                    }
                }
                
                var hierarchyStatusMessage = new WebSocketMessage
                {
                    Type = "hierarchy_status",
                    Data = new
                    {
                        pointId = pointId,
                        directSubscribers = pointSubscribers.Count,
                        totalConnections = connections.Count,
                        directSubscriberIds = pointSubscribers,
                        allConnectionIds = connections,
                        deviceInfo = deviceInfo,
                        groupInfo = groupInfo
                    },
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                };
                await SendToConnectionAsync(connectionId, hierarchyStatusMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取层级关系状态时发生错误 - 连接ID: {ConnectionId}, 点位ID: {PointId}", connectionId, pointId);
                var errorMessage = WebSocketMessage.CreateError($"获取点位{pointId}层级关系状态失败", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
        }

        /// <summary>
        /// 处理测试数据推送命令
        /// </summary>
        private async Task HandleTestDataPushAsync(string connectionId)
        {
            try
            {
                // 创建测试数据推送消息
                var testMessage = new
                {
                    Type = "PointUpdate",
                    PointId = 999,
                    Value = "测试值_" + DateTime.Now.ToString("HH:mm:ss"),
                    Status = "Good",
                    UpdateTime = DateTime.UtcNow
                };
                
                // 通过数据推送总线发布测试消息
                await _dataPushBus.PublishAsync(testMessage);
                
                // 发送测试确认消息
                var confirmMessage = new WebSocketMessage
                {
                    Type = "test_data_push_sent",
                    Data = new
                    {
                        message = "测试数据推送已发送",
                        testData = testMessage
                    },
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                };
                await SendToConnectionAsync(connectionId, confirmMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试数据推送时发生错误 - 连接ID: {ConnectionId}", connectionId);
                var errorMessage = WebSocketMessage.CreateError("测试数据推送失败", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
        }

        /// <summary>
        /// 处理获取点位状态命令
        /// </summary>
        private async Task HandleGetPointStatusAsync(string connectionId, int pointId)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var timeWheelScheduler = scope.ServiceProvider.GetService<TimeWheelScheduler>();
                    var devicePointRepository = scope.ServiceProvider.GetService<Domain.Repositories.IDevicePointRepository>();

                    // 检查点位是否在调度器中
                    bool isRegistered = false;
                    object scheduledInfo = null;
                    if (timeWheelScheduler != null)
                    {
                        isRegistered = timeWheelScheduler.IsPointRegistered(pointId);
                        if (isRegistered)
                        {
                            var scheduledPoint = timeWheelScheduler.GetScheduledPoint(pointId);
                            if (scheduledPoint != null)
                            {
                                scheduledInfo = new
                                {
                                    pointId = scheduledPoint.PointId,
                                    deviceId = scheduledPoint.DeviceId,
                                    channelId = scheduledPoint.ChannelId,
                                    address = scheduledPoint.Address,
                                    scanRate = scheduledPoint.ScanRate,
                                    nextExecutionTime = scheduledPoint.NextExecutionTime,
                                    lastExecutionTime = scheduledPoint.LastExecutionTime,
                                    executionCount = scheduledPoint.ExecutionCount,
                                    errorCount = scheduledPoint.ErrorCount
                                };
                            }
                        }
                    }

                    // 获取点位缓存数据
                    var cacheManager = scope.ServiceProvider.GetService<CacheManager>();
                    var cachedValue = cacheManager?.GetCachedValue(pointId);

                    // 获取数据库中的点位信息
                    object pointInfo = null;
                    if (devicePointRepository != null)
                    {
                        var point = await devicePointRepository.GetDevicePointAsync(pointId);
                        if (point != null)
                        {
                            pointInfo = new
                            {
                                id = point.Id,
                                name = point.Name,
                                address = point.Address,
                                enable = point.Enable,
                                scanRate = point.ScanRate,
                                value = point.Value,
                                deviceId = point.DeviceId
                            };
                        }
                    }

                    var pointStatusMessage = new WebSocketMessage
                    {
                        Type = "point_status",
                        Data = new
                        {
                            pointId = pointId,
                            isRegisteredInScheduler = isRegistered,
                            scheduledInfo = scheduledInfo,
                            cachedValue = cachedValue.HasValue ? new
                            {
                                value = cachedValue.Value.Value,
                                status = cachedValue.Value.Status.ToString(),
                                updateTime = cachedValue.Value.UpdateTime
                            } : null,
                            pointInfo = pointInfo
                        },
                        ConnectionId = connectionId,
                        Timestamp = DateTime.UtcNow
                    };
                    await SendToConnectionAsync(connectionId, pointStatusMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取点位状态时发生错误 - 连接ID: {ConnectionId}, 点位ID: {PointId}", connectionId, pointId);
                var errorMessage = WebSocketMessage.CreateError($"获取点位{pointId}状态失败", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
        }

        /// <summary>
        /// 处理获取点位订阅详情命令
        /// </summary>
        private async Task HandleGetPointSubscriptionDetailsAsync(string connectionId, int pointId)
        {
            try
            {
                // 获取该连接的详细订阅信息
                var details = _subscriptionManager.GetPointSubscriptionDetails(pointId, connectionId);
                
                var detailsMessage = new WebSocketMessage
                {
                    Type = "point_subscription_details",
                    Data = new
                    {
                        pointId = details.PointId,
                        connectionId = details.ConnectionId,
                        deviceId = details.DeviceId,
                        groupId = details.GroupId,
                        willReceiveUpdates = details.WillReceiveUpdates,
                        subscriptionReasons = details.SubscriptionReasons,
                        directPointSubscribers = details.DirectPointSubscribers,
                        deviceSubscribers = details.DeviceSubscribers,
                        groupSubscribers = details.GroupSubscribers,
                        allSubscribers = details.AllSubscribers,
                        statistics = new
                        {
                            totalSubscriberCount = details.TotalSubscriberCount,
                            directSubscriberCount = details.DirectSubscriberCount,
                            deviceSubscriberCount = details.DeviceSubscriberCount,
                            groupSubscriberCount = details.GroupSubscriberCount
                        },
                        explanation = details.WillReceiveUpdates 
                            ? $"您的连接会收到点位{pointId}的数据推送，原因：{string.Join("；", details.SubscriptionReasons)}"
                            : $"您的连接不会收到点位{pointId}的数据推送，因为您没有订阅该点位、其所属设备或设备组"
                    },
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                };
                await SendToConnectionAsync(connectionId, detailsMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取点位订阅详情时发生错误 - 连接ID: {ConnectionId}, 点位ID: {PointId}", connectionId, pointId);
                var errorMessage = WebSocketMessage.CreateError($"获取点位{pointId}订阅详情失败", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
        }

        /// <summary>
        /// 处理获取设备数据命令
        /// </summary>
        private async Task HandleGetDeviceDataAsync(string connectionId, int deviceId)
        {
            try
            {
                // 获取设备数据快照
                var deviceSnapshot = await _subscriptionManager.GetDeviceDataSnapshotAsync(deviceId);
                
                var deviceDataMessage = new WebSocketMessage
                {
                    Type = "device_data",
                    Data = new
                    {
                        deviceId = deviceSnapshot.DeviceId,
                        deviceName = deviceSnapshot.DeviceName,
                        deviceEnable = deviceSnapshot.DeviceEnable,
                        channelId = deviceSnapshot.ChannelId,
                        pointCount = deviceSnapshot.Points.Count,
                        snapshotTime = deviceSnapshot.SnapshotTime,
                        points = deviceSnapshot.Points.Select(p => new
                        {
                            pointId = p.PointId,
                            name = p.Name,
                            address = p.Address,
                            value = p.Value,
                            status = p.Status,
                            updateTime = p.UpdateTime,
                            enable = p.Enable,
                            scanRate = p.ScanRate
                        }).ToList()
                    },
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                };
                await SendToConnectionAsync(connectionId, deviceDataMessage);
                
                _logger.LogDebug("设备 {DeviceId} 数据已推送到连接 {ConnectionId}，包含 {PointCount} 个点位", 
                    deviceId, connectionId, deviceSnapshot.Points.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备数据时发生错误 - 连接ID: {ConnectionId}, 设备ID: {DeviceId}", connectionId, deviceId);
                var errorMessage = WebSocketMessage.CreateError($"获取设备{deviceId}数据失败", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
        }

        /// <summary>
        /// 处理获取设备组数据命令
        /// </summary>
        private async Task HandleGetGroupDataAsync(string connectionId, int groupId)
        {
            try
            {
                // 获取设备组数据快照
                var groupSnapshot = await _subscriptionManager.GetGroupDataSnapshotAsync(groupId);
                
                var groupDataMessage = new WebSocketMessage
                {
                    Type = "group_data",
                    Data = new
                    {
                        groupId = groupSnapshot.GroupId,
                        groupName = groupSnapshot.GroupName,
                        deviceCount = groupSnapshot.TotalDeviceCount,
                        pointCount = groupSnapshot.TotalPointCount,
                        snapshotTime = groupSnapshot.SnapshotTime,
                        devices = groupSnapshot.Devices.Select(d => new
                        {
                            deviceId = d.DeviceId,
                            deviceName = d.DeviceName,
                            deviceEnable = d.DeviceEnable,
                            channelId = d.ChannelId,
                            pointCount = d.Points.Count,
                            points = d.Points.Select(p => new
                            {
                                pointId = p.PointId,
                                name = p.Name,
                                address = p.Address,
                                value = p.Value,
                                status = p.Status,
                                updateTime = p.UpdateTime,
                                enable = p.Enable,
                                scanRate = p.ScanRate
                            }).ToList()
                        }).ToList()
                    },
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                };
                await SendToConnectionAsync(connectionId, groupDataMessage);
                
                _logger.LogDebug("设备组 {GroupId} 数据已推送到连接 {ConnectionId}，包含 {DeviceCount} 个设备，{PointCount} 个点位", 
                    groupId, connectionId, groupSnapshot.TotalDeviceCount, groupSnapshot.TotalPointCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备组数据时发生错误 - 连接ID: {ConnectionId}, 设备组ID: {GroupId}", connectionId, groupId);
                var errorMessage = WebSocketMessage.CreateError($"获取设备组{groupId}数据失败", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
        }

        /// <summary>
        /// 处理获取所有连接详情命令
        /// </summary>
        private async Task HandleGetAllConnectionDetailsAsync(string connectionId)
        {
            try
            {
                var allDetails = _subscriptionManager.GetAllConnectionDetails();
                
                var detailsMessage = new WebSocketMessage
                {
                    Type = "all_connection_details",
                    Data = new
                    {
                        totalConnections = allDetails.Count,
                        connections = allDetails.Select(detail => new
                        {
                            connectionId = detail.ConnectionId,
                            lastActivityTime = detail.LastActivityTime,
                            totalSubscriptions = detail.TotalSubscriptions,
                            groupSubscriptions = detail.GroupSubscriptions,
                            deviceSubscriptions = detail.DeviceSubscriptions,
                            pointSubscriptions = detail.PointSubscriptions
                        }).ToList()
                    },
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                };
                await SendToConnectionAsync(connectionId, detailsMessage);
                
                _logger.LogDebug("所有连接详情已推送到连接 {ConnectionId}，总连接数: {Count}", connectionId, allDetails.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有连接详情时发生错误 - 连接ID: {ConnectionId}", connectionId);
                var errorMessage = WebSocketMessage.CreateError("获取所有连接详情失败", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
            }
        }

        /// <summary>
        /// 处理强制清理订阅命令
        /// </summary>
        private async Task HandleForceCleanupSubscriptionAsync(string connectionId, WebSocketCommand command)
        {
            try
            {
                var targetConnectionId = command.ConnectionId ?? connectionId; // 如果没有指定目标连接，则清理当前连接
                var result = _subscriptionManager.ForceCleanupSubscription(targetConnectionId, command.SubscriptionType!, command.Id > 0 ? command.Id : null);
                
                var cleanupMessage = new WebSocketMessage
                {
                    Type = "force_cleanup_result",
                    Data = new
                    {
                        targetConnectionId = targetConnectionId,
                        subscriptionType = command.SubscriptionType,
                        targetId = command.Id > 0 ? (int?)command.Id : null,
                        success = result.Success,
                        message = result.Message,
                        removedCount = result.RemovedCount,
                        details = result.Details
                    },
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                };
                await SendToConnectionAsync(connectionId, cleanupMessage);
                
                _logger.LogInformation("强制清理订阅完成 - 目标连接: {TargetConnectionId}, 类型: {SubscriptionType}, 成功: {Success}", 
                    targetConnectionId, command.SubscriptionType, result.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制清理订阅时发生错误 - 连接ID: {ConnectionId}, 类型: {SubscriptionType}", connectionId, command.SubscriptionType);
                var errorMessage = WebSocketMessage.CreateError($"强制清理订阅失败: {ex.Message}", connectionId);
                await SendToConnectionAsync(connectionId, errorMessage);
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