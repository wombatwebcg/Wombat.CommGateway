using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Hubs;
using Wombat.CommGateway.Application.Services.DataCollection;
using Wombat.CommGateway.Domain.Enums;
using Wombat.Extensions.AutoGenerator.Attributes;
using System.Threading;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 统一数据分发服务实现
    /// 负责将数据更新按订阅规则分发到SignalR和WebSocket
    /// 使用订阅模式，避免全局广播和重复推送
    /// </summary>
    [AutoInject<IDataDistributionService>(ServiceLifetime = ServiceLifetime.Singleton)]
    public class DataDistributionService : IDataDistributionService
    {
        private readonly IHubContext<DataCollectionHub> _hubContext;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ILogger<DataDistributionService> _logger;
        private readonly WebSocketService _webSocketService;
        
        // 统计信息
        private long _totalDistributedMessages = 0;
        private DateTime _lastDistributionTime = DateTime.MinValue;
        private readonly object _statsLock = new();

        public DataDistributionService(
            IHubContext<DataCollectionHub> hubContext,
            ISubscriptionManager subscriptionManager,
            ILogger<DataDistributionService> logger,
            WebSocketService webSocketService)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
        }

        /// <summary>
        /// 分发单个点位数据更新
        /// </summary>
        public async Task DistributePointUpdateAsync(int pointId, string value, DataPointStatus status, DateTime updateTime)
        {
            try
            {
                _logger.LogDebug("开始分发点位 {PointId} 更新: 值={Value}, 状态={Status}", pointId, value, status);

                // 获取所有需要接收此点位更新的连接（包括层级订阅）
                var subscribedConnections = _subscriptionManager.GetConnectionsForPointUpdate(pointId);
                
                if (!subscribedConnections.Any())
                {
                    _logger.LogDebug("点位 {PointId} 没有订阅者，跳过推送", pointId);
                    return;
                }

                var message = new
                {
                    Type = "PointUpdate",
                    PointId = pointId,
                    Value = value,
                    Status = status.ToString(),
                    UpdateTime = updateTime
                };

                // 并行分发到不同通道
                var tasks = new List<Task>
                {
                    DistributeToSignalRAsync(subscribedConnections, "ReceivePointUpdate", message),
                    DistributeToWebSocketAsync(subscribedConnections, message)
                };

                await Task.WhenAll(tasks);

                // 更新统计信息
                UpdateStatistics(subscribedConnections.Count);

                _logger.LogDebug("点位 {PointId} 更新已分发到 {Count} 个连接", pointId, subscribedConnections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分发点位 {PointId} 更新时发生错误", pointId);
            }
        }

        /// <summary>
        /// 分发批量点位数据更新
        /// 按连接分组进行高效批量推送，既保持订阅精确性又保持推送效率
        /// </summary>
        public async Task DistributeBatchPointsUpdateAsync(Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)> updates)
        {
            try
            {
                _logger.LogDebug("开始分发批量点位更新，共 {Count} 个点位", updates.Count);

                if (!updates.Any())
                {
                    return;
                }

                // 按连接分组：每个连接收集其订阅的所有点位更新
                var connectionUpdatesMap = new Dictionary<string, List<object>>();
                var totalPushCount = 0;

                foreach (var (pointId, (value, status, updateTime)) in updates)
                {
                    // 获取该点位的订阅连接
                    var subscribedConnections = _subscriptionManager.GetConnectionsForPointUpdate(pointId);
                    
                    if (!subscribedConnections.Any())
                    {
                        _logger.LogDebug("点位 {PointId} 没有订阅者，跳过推送", pointId);
                        continue;
                    }

                    // 为每个订阅该点位的连接添加更新数据
                    var pointUpdate = new
                    {
                        PointId = pointId,
                        Value = value,
                        Status = status.ToString(),
                        UpdateTime = updateTime
                    };

                    foreach (var connectionId in subscribedConnections)
                    {
                        if (!connectionUpdatesMap.ContainsKey(connectionId))
                        {
                            connectionUpdatesMap[connectionId] = new List<object>();
                        }
                        connectionUpdatesMap[connectionId].Add(pointUpdate);
                        totalPushCount++;
                    }
                }

                if (!connectionUpdatesMap.Any())
                {
                    _logger.LogDebug("批量点位更新没有任何订阅者，跳过推送");
                    return;
                }

                // 为每个连接创建其专属的批量更新消息并并行推送
                var distributionTasks = new List<Task>();

                foreach (var (connectionId, updates_list) in connectionUpdatesMap)
                {
                    // 如果连接只有一个点位更新，使用单点位消息格式
                    if (updates_list.Count == 1)
                    {
                        var singleUpdate = updates_list[0];
                        distributionTasks.Add(DistributeToSignalRAsync(
                            new[] { connectionId }, 
                            "ReceivePointUpdate", 
                            singleUpdate));
                        distributionTasks.Add(DistributeToWebSocketAsync(
                            new[] { connectionId }, 
                            singleUpdate));
                        
                        _logger.LogDebug("连接 {ConnectionId} 单点位更新推送准备", connectionId);
                    }
                    else
                    {
                        // 如果连接有多个点位更新，使用批量消息格式
                        var batchMessage = new
                        {
                            Type = "BatchPointsUpdate",
                            Updates = updates_list,
                            UpdateTime = DateTime.UtcNow,
                            Count = updates_list.Count
                        };

                        distributionTasks.Add(DistributeToSignalRAsync(
                            new[] { connectionId }, 
                            "ReceiveBatchPointsUpdate", 
                            batchMessage));
                        distributionTasks.Add(DistributeToWebSocketAsync(
                            new[] { connectionId }, 
                            batchMessage));
                        
                        _logger.LogDebug("连接 {ConnectionId} 批量更新推送准备，包含 {Count} 个点位", 
                            connectionId, updates_list.Count);
                    }
                }

                // 并行执行所有推送任务
                await Task.WhenAll(distributionTasks);

                // 更新统计信息
                UpdateStatistics(totalPushCount);

                _logger.LogInformation("批量点位更新分发完成：{OriginalCount} 个点位更新，推送到 {ConnectionCount} 个连接，总推送次数 {TotalPush}", 
                    updates.Count, connectionUpdatesMap.Count, totalPushCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分发批量点位更新时发生错误");
            }
        }

        /// <summary>
        /// 分发点位状态变更
        /// </summary>
        public async Task DistributePointStatusChangeAsync(int pointId, DataPointStatus status)
        {
            try
            {
                var subscribedConnections = _subscriptionManager.GetConnectionsForPointUpdate(pointId);
                
                if (!subscribedConnections.Any())
                {
                    _logger.LogDebug("点位 {PointId} 状态变更没有订阅者，跳过推送", pointId);
                    return;
                }

                var message = new
                {
                    Type = "PointStatusChange",
                    PointId = pointId,
                    Status = status.ToString(),
                    UpdateTime = DateTime.UtcNow
                };

                var tasks = new List<Task>
                {
                    DistributeToSignalRAsync(subscribedConnections, "ReceivePointStatusChange", message),
                    DistributeToWebSocketAsync(subscribedConnections, message)
                };

                await Task.WhenAll(tasks);

                UpdateStatistics(subscribedConnections.Count);

                _logger.LogDebug("点位 {PointId} 状态变更已分发到 {Count} 个连接", pointId, subscribedConnections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分发点位 {PointId} 状态变更时发生错误", pointId);
            }
        }

        /// <summary>
        /// 分发点位移除通知
        /// </summary>
        public async Task DistributePointRemovedAsync(int pointId)
        {
            try
            {
                // 在点位被移除之前获取订阅者
                var subscribedConnections = _subscriptionManager.GetConnectionsForPointUpdate(pointId);
                
                if (!subscribedConnections.Any())
                {
                    _logger.LogDebug("点位 {PointId} 移除通知没有订阅者，跳过推送", pointId);
                    return;
                }

                var message = new
                {
                    Type = "PointRemoved",
                    PointId = pointId,
                    UpdateTime = DateTime.UtcNow
                };

                var tasks = new List<Task>
                {
                    DistributeToSignalRAsync(subscribedConnections, "ReceivePointRemoved", message),
                    DistributeToWebSocketAsync(subscribedConnections, message)
                };

                await Task.WhenAll(tasks);

                UpdateStatistics(subscribedConnections.Count);

                _logger.LogInformation("点位 {PointId} 移除通知已分发到 {Count} 个连接", pointId, subscribedConnections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分发点位 {PointId} 移除通知时发生错误", pointId);
            }
        }

        /// <summary>
        /// 分发批量点位移除通知
        /// </summary>
        public async Task DistributeBatchPointsRemovedAsync(IEnumerable<int> pointIds)
        {
            try
            {
                var pointIdList = pointIds.ToList();
                if (!pointIdList.Any())
                {
                    return;
                }

                _logger.LogDebug("开始分发批量点位移除通知，共 {Count} 个点位", pointIdList.Count);

                // 获取所有相关的连接
                var allConnections = _subscriptionManager.GetConnectionsForPointUpdates(pointIdList);
                
                if (!allConnections.Any())
                {
                    _logger.LogDebug("批量点位移除通知没有订阅者，跳过推送");
                    return;
                }

                var message = new
                {
                    Type = "BatchPointsRemoved",
                    PointIds = pointIdList,
                    UpdateTime = DateTime.UtcNow
                };

                var tasks = new List<Task>
                {
                    DistributeToSignalRAsync(allConnections, "ReceiveBatchPointsRemoved", message),
                    DistributeToWebSocketAsync(allConnections, message)
                };

                await Task.WhenAll(tasks);

                UpdateStatistics(allConnections.Count);

                _logger.LogInformation("批量点位移除通知已分发，{PointCount} 个点位推送到 {ConnectionCount} 个连接", 
                    pointIdList.Count, allConnections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分发批量点位移除通知时发生错误");
            }
        }

        /// <summary>
        /// 获取分发统计信息
        /// </summary>
        public async Task<DistributionStatistics> GetDistributionStatisticsAsync()
        {
            try
            {
                var allConnections = _subscriptionManager.GetAllConnections();
                var totalSubscriptions = 0;

                foreach (var connectionId in allConnections)
                {
                    var status = _subscriptionManager.GetConnectionStatus(connectionId);
                    totalSubscriptions += status.TotalSubscriptions;
                }

                lock (_statsLock)
                {
                    return new DistributionStatistics
                    {
                        SignalRConnections = allConnections.Count, // 假设SignalR和WebSocket使用相同的连接管理
                        WebSocketConnections = _webSocketService.GetConnectionCount(),
                        TotalSubscriptions = totalSubscriptions,
                        TotalDistributedMessages = _totalDistributedMessages,
                        LastDistributionTime = _lastDistributionTime,
                        ChannelDetails = new Dictionary<string, int>
                        {
                            { "SignalR", allConnections.Count },
                            { "WebSocket", _webSocketService.GetConnectionCount() }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分发统计信息时发生错误");
                return new DistributionStatistics();
            }
        }



        #region 私有方法

        /// <summary>
        /// 分发到SignalR
        /// </summary>
        private async Task DistributeToSignalRAsync(IReadOnlyList<string> connectionIds, string method, object message)
        {
            try
            {
                if (connectionIds.Any())
                {
                    await _hubContext.Clients.Clients(connectionIds).SendAsync(method, message);
                    _logger.LogDebug("SignalR分发完成: {Method} -> {Count} 个连接", method, connectionIds.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR分发失败: {Method}", method);
            }
        }

        /// <summary>
        /// 分发到WebSocket
        /// </summary>
        private async Task DistributeToWebSocketAsync(IReadOnlyList<string> connectionIds, object message)
        {
            try
            {
                var wsMessage = new WebSocketMessage
                {
                    Type = GetMessageType(message),
                    Data = message,
                    Timestamp = DateTime.UtcNow
                };

                var tasks = connectionIds.Select(connectionId =>
                {
                    wsMessage.ConnectionId = connectionId;
                    return _webSocketService.SendToConnectionAsync(connectionId, wsMessage);
                });

                await Task.WhenAll(tasks);
                _logger.LogDebug("WebSocket分发完成: {Type} -> {Count} 个连接", wsMessage.Type, connectionIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket分发失败");
            }
        }

        /// <summary>
        /// 从消息对象获取消息类型
        /// </summary>
        private string GetMessageType(object message)
        {
            try
            {
                var typeProperty = message.GetType().GetProperty("Type");
                return typeProperty?.GetValue(message)?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics(int messageCount)
        {
            lock (_statsLock)
            {
                _totalDistributedMessages += messageCount;
                _lastDistributionTime = DateTime.UtcNow;
            }
        }

        #endregion
    }

    /// <summary>
    /// WebSocket消息结构
    /// </summary>
    public class WebSocketMessage
    {
        public string Type { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? ConnectionId { get; set; }
        public DateTime Timestamp { get; set; }

        public static WebSocketMessage CreateConnectionStatus(string connectionId, object data)
        {
            return new WebSocketMessage
            {
                Type = "connection_status",
                Data = data,
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };
        }

        public static WebSocketMessage CreateError(string error, string connectionId)
        {
            return new WebSocketMessage
            {
                Type = "error",
                Data = new { error },
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };
        }

        public static WebSocketMessage CreatePong()
        {
            return new WebSocketMessage
            {
                Type = "pong",
                Data = new { message = "pong" },
                Timestamp = DateTime.UtcNow
            };
        }

        public static WebSocketMessage CreateSubscriptionConfirmed(string target, int id, string connectionId)
        {
            return new WebSocketMessage
            {
                Type = "subscription_confirmed",
                Data = new { target, id },
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };
        }

        public static WebSocketMessage CreateUnsubscriptionConfirmed(string target, int id, string connectionId)
        {
            return new WebSocketMessage
            {
                Type = "unsubscription_confirmed",
                Data = new { target, id },
                ConnectionId = connectionId,
                Timestamp = DateTime.UtcNow
            };
        }
    }
} 