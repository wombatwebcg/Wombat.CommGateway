using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Hubs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Enums;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.Services.DataCollection;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 数据采集Hub服务
    /// 实现缓存更新通知服务，负责将缓存更新转发到SignalR Hub
    /// </summary>
    /// 


    [AutoInject<ICacheUpdateNotificationService>(ServiceLifetime = ServiceLifetime.Singleton)]
    public class DataCollectionHubService : ICacheUpdateNotificationService
    {
        private readonly IHubContext<DataCollectionHub> _hubContext;
        private readonly ILogger<DataCollectionHubService> _logger;
        private readonly IDataPushBus _dataPushBus;
        private readonly Queue<(int PointId, string Value, DataPointStatus Status, DateTime UpdateTime)> _updateQueue;
        private readonly object _queueLock = new object();
        private readonly System.Timers.Timer _batchTimer;
        private const int BatchSize = 100;
        private const int BatchIntervalMs = 1000; // 1秒

        public DataCollectionHubService(
            IHubContext<DataCollectionHub> hubContext,
            ILogger<DataCollectionHubService> logger,
            IDataPushBus dataPushBus)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataPushBus = dataPushBus ?? throw new ArgumentNullException(nameof(dataPushBus));
            
            _updateQueue = new Queue<(int, string, DataPointStatus, DateTime)>();
            
            // 初始化批量推送定时器
            _batchTimer = new System.Timers.Timer(BatchIntervalMs);
            _batchTimer.Elapsed += async (sender, e) => await ProcessBatchUpdatesAsync();
            _batchTimer.AutoReset = true;
            _batchTimer.Start();
            
            _logger.LogInformation("DataCollectionHubService 已初始化，批量推送间隔: {BatchIntervalMs}ms", BatchIntervalMs);
        }

        /// <summary>
        /// 通知单个点位数据更新
        /// </summary>
        public async Task OnPointDataUpdatedAsync(int pointId, string value, DataPointStatus status, DateTime updateTime)
        {
            try
            {
                // 添加到批量推送队列
                lock (_queueLock)
                {
                    _updateQueue.Enqueue((pointId, value, status, updateTime));
                    
                    // 如果队列达到批量大小，立即处理
                    if (_updateQueue.Count >= BatchSize)
                    {
                        _ = Task.Run(async () => await ProcessBatchUpdatesAsync());
                    }
                }

                // 同时推送单个更新（用于实时性要求高的场景）
                await PushSinglePointUpdateAsync(pointId, value, status, updateTime);
                
                // 发布到WebSocket桥接
                var message = new
                {
                    Type = "PointUpdate",
                    PointId = pointId,
                    Value = value,
                    Status = status.ToString(),
                    UpdateTime = updateTime
                };
                await _dataPushBus.PublishAsync(message);
                _logger.LogDebug("已发布点位 {PointId} 更新到WebSocket桥接", pointId);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"推送点位 {pointId} 数据更新时发生错误");
            }
        }

        /// <summary>
        /// 通知批量点位数据更新
        /// </summary>
        public async Task OnBatchPointsDataUpdatedAsync(Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)> updates)
        {
            try
            {
                var updateList = updates.Select(kvp => new
                {
                    PointId = kvp.Key,
                    Value = kvp.Value.Value,
                    Status = kvp.Value.Status.ToString(),
                    UpdateTime = kvp.Value.UpdateTime
                }).ToList();

                var message = new
                {
                    Type = "BatchPointsUpdate",
                    Updates = updateList,
                    UpdateTime = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("ReceiveBatchPointsUpdate", message);

                // 发布到WebSocket桥接
                await _dataPushBus.PublishAsync(message);
                _logger.LogDebug("已发布批量点位更新到WebSocket桥接，共 {Count} 个点位", updates.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量推送点位数据更新时发生错误");
            }
        }

        /// <summary>
        /// 通知点位状态变更
        /// </summary>
        public async Task OnPointStatusChangedAsync(int pointId, DataPointStatus status)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceivePointStatusChange", new
                {
                    Type = "PointStatusChange",
                    PointId = pointId,
                    Status = status.ToString(),
                    UpdateTime = DateTime.UtcNow
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"推送点位 {pointId} 状态变更时发生错误");
            }
        }

        /// <summary>
        /// 通知点位被移除
        /// </summary>
        public async Task OnPointRemovedAsync(int pointId)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceivePointRemoved", new
                {
                    Type = "PointRemoved",
                    PointId = pointId,
                    UpdateTime = DateTime.UtcNow
                });

                _logger.LogInformation($"推送点位 {pointId} 移除通知");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"推送点位 {pointId} 移除通知时发生错误");
            }
        }

        /// <summary>
        /// 通知批量点位被移除
        /// </summary>
        public async Task OnBatchPointsRemovedAsync(IEnumerable<int> pointIds)
        {
            try
            {
                var pointIdList = pointIds.ToList();
                
                await _hubContext.Clients.All.SendAsync("ReceiveBatchPointsRemoved", new
                {
                    Type = "BatchPointsRemoved",
                    PointIds = pointIdList,
                    UpdateTime = DateTime.UtcNow
                });

                _logger.LogInformation($"批量推送 {pointIdList.Count} 个点位移除通知");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量推送点位移除通知时发生错误");
            }
        }

        /// <summary>
        /// 推送单个点位更新
        /// </summary>
        private async Task PushSinglePointUpdateAsync(int pointId, string value, DataPointStatus status, DateTime updateTime)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceivePointUpdate", new
                {
                    Type = "PointUpdate",
                    PointId = pointId,
                    Value = value,
                    Status = status.ToString(),
                    UpdateTime = updateTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"推送单个点位 {pointId} 更新时发生错误");
            }
        }

        /// <summary>
        /// 处理批量更新队列
        /// </summary>
        private async Task ProcessBatchUpdatesAsync()
        {
            List<(int PointId, string Value, DataPointStatus Status, DateTime UpdateTime)> batchUpdates;
            
            lock (_queueLock)
            {
                if (_updateQueue.Count == 0)
                    return;

                batchUpdates = new List<(int, string, DataPointStatus, DateTime)>();
                while (_updateQueue.Count > 0 && batchUpdates.Count < BatchSize)
                {
                    batchUpdates.Add(_updateQueue.Dequeue());
                }
            }

            if (batchUpdates.Count == 0)
                return;

            try
            {
                var updateList = batchUpdates.Select(update => new
                {
                    PointId = update.PointId,
                    Value = update.Value,
                    Status = update.Status.ToString(),
                    UpdateTime = update.UpdateTime
                }).ToList();

                await _hubContext.Clients.All.SendAsync("ReceiveBatchPointsUpdate", new
                {
                    Type = "BatchPointsUpdate",
                    Updates = updateList,
                    UpdateTime = DateTime.UtcNow
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理批量更新队列时发生错误");
            }
        }

        /// <summary>
        /// 获取当前队列状态
        /// </summary>
        public (int QueueCount, int BatchSize, int BatchIntervalMs) GetQueueStatus()
        {
            lock (_queueLock)
            {
                return (_updateQueue.Count, BatchSize, BatchIntervalMs);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            _batchTimer?.Stop();
            _batchTimer?.Dispose();
        }
    }
} 