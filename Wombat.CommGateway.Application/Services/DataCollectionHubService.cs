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
        private readonly IDataDistributionService _dataDistributionService;
        private readonly ILogger<DataCollectionHubService> _logger;
        private readonly Queue<(int PointId, string Value, DataPointStatus Status, DateTime UpdateTime)> _updateQueue;
        private readonly object _queueLock = new object();
        private readonly System.Timers.Timer _batchTimer;
        private const int BatchSize = 100;
        private const int BatchIntervalMs = 1000; // 1秒

        public DataCollectionHubService(
            IDataDistributionService dataDistributionService,
            ILogger<DataCollectionHubService> logger)
        {
            _dataDistributionService = dataDistributionService ?? throw new ArgumentNullException(nameof(dataDistributionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _updateQueue = new Queue<(int, string, DataPointStatus, DateTime)>();
            
            // 初始化批量推送定时器
            _batchTimer = new System.Timers.Timer(BatchIntervalMs);
            _batchTimer.Elapsed += async (sender, e) => await ProcessBatchUpdatesAsync();
            _batchTimer.AutoReset = true;
            _batchTimer.Start();
            
            _logger.LogInformation("DataCollectionHubService 已初始化，使用统一数据分发服务，批量推送间隔: {BatchIntervalMs}ms", BatchIntervalMs);
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

                // 通过统一数据分发服务推送更新（同时分发到SignalR和WebSocket）
                await _dataDistributionService.DistributePointUpdateAsync(pointId, value, status, updateTime);
                
                _logger.LogDebug("已通过统一分发服务推送点位 {PointId} 更新", pointId);
                
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
                _logger.LogInformation($"收到批量点位数据更新通知: {updates.Count} 个点位");
                
                // 通过统一数据分发服务推送批量更新（同时分发到SignalR和WebSocket）
                await _dataDistributionService.DistributeBatchPointsUpdateAsync(updates);
                
                _logger.LogInformation($"已通过统一分发服务推送 {updates.Count} 个点位数据更新");

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
                // 通过统一数据分发服务推送状态变更
                await _dataDistributionService.DistributePointStatusChangeAsync(pointId, status);
                
                _logger.LogDebug("已通过统一分发服务推送点位 {PointId} 状态变更", pointId);
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
                // 通过统一数据分发服务推送点位移除通知
                await _dataDistributionService.DistributePointRemovedAsync(pointId);

                _logger.LogInformation($"已通过统一分发服务推送点位 {pointId} 移除通知");
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
                
                // 通过统一数据分发服务推送批量点位移除通知
                await _dataDistributionService.DistributeBatchPointsRemovedAsync(pointIdList);

                _logger.LogInformation($"已通过统一分发服务推送 {pointIdList.Count} 个点位移除通知");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量推送点位移除通知时发生错误");
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
                // 转换为Dictionary格式，供统一分发服务使用
                var updates = batchUpdates.ToDictionary(
                    update => update.PointId,
                    update => (update.Value, update.Status, update.UpdateTime)
                );

                // 通过统一数据分发服务推送批量更新
                await _dataDistributionService.DistributeBatchPointsUpdateAsync(updates);
                
                _logger.LogDebug("批量更新队列处理完成，推送了 {Count} 个点位更新", batchUpdates.Count);
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