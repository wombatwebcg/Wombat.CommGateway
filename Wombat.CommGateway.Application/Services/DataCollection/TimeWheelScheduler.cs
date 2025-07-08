using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wombat.CommGateway.Domain.Entities;
using Wombat.Extensions.AutoGenerator.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Wombat.CommGateway.Application.Services.DataCollection.Models;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    /// <summary>
    /// 基于时间轮算法的高效调度器
    /// 支持毫秒级精度调度，按(ScanRate, ChannelId)组合分组执行
    /// 确保不同通道的点位使用独立的客户端连接处理
    /// </summary>
    [AutoInject<TimeWheelScheduler>(ServiceLifetime.Singleton)]
    public class TimeWheelScheduler : IDataCollectionScheduler, IDisposable
    {
        private readonly ILogger<TimeWheelScheduler> _logger;
        private readonly ConcurrentDictionary<int, ScheduledPoint> _scheduledPoints;
        private readonly ConcurrentDictionary<string, List<ScheduledPoint>> _scanRateGroups;
        private readonly Timer _timer;
        private readonly object _lockObject = new object();
        private volatile bool _isRunning;
        private readonly SchedulerStatistics _statistics;
        private readonly int _tickIntervalMs = 10; // 10毫秒一个tick

        // 事件：当需要执行采集任务时触发
        public event Func<CollectionTask, Task> OnCollectionTaskReady;

        public bool IsRunning => _isRunning;
        public int ScheduledPointCount => _scheduledPoints.Count;

        public TimeWheelScheduler(ILogger<TimeWheelScheduler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statistics = new SchedulerStatistics();
            _scanRateGroups = new ConcurrentDictionary<string, List<ScheduledPoint>>();
            _scheduledPoints = new ConcurrentDictionary<int, ScheduledPoint>();
            _timer = new Timer(ProcessTimeWheel, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// 检查点位是否已注册
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>是否已注册</returns>
        public bool IsPointRegistered(int pointId)
        {
            return _scheduledPoints.ContainsKey(pointId);
        }

        /// <summary>
        /// 批量注册点位
        /// </summary>
        /// <param name="points">要注册的点位列表</param>
        /// <returns>成功注册的点位数量</returns>
        public async Task<int> BatchRegisterPointsAsync(IEnumerable<DevicePoint> points)
        {
            if (points == null)
                return 0;

            var pointArray = points.ToArray();
            var successCount = 0;

            _logger.LogInformation($"开始批量注册 {pointArray.Length} 个点位");

            foreach (var point in pointArray)
            {
                if (point == null)
                    continue;

                try
                {
                    // 检查点位是否已存在
                    if (IsPointRegistered(point.Id))
                    {
                        continue;
                    }

                    var scheduledPoint = new ScheduledPoint
                    {
                        PointId = point.Id,
                        DeviceId = point.DeviceId,
                        ChannelId = point.Device?.ChannelId ?? 0,
                        Address = point.Address,
                        DataType = point.DataType,
                        ScanRate = point.ScanRate,
                        ReadWrite = point.ReadWrite,
                        NextExecutionTime = DateTime.UtcNow.AddMilliseconds(point.ScanRate),
                        LastExecutionTime = DateTime.MinValue,
                        ExecutionCount = 0,
                        ErrorCount = 0
                    };

                    _scheduledPoints[point.Id] = scheduledPoint;
                    AddToScanRateGroup(scheduledPoint);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"批量注册点位 {point.Id} 失败");
                }
            }

            _logger.LogInformation($"批量注册完成: 成功注册 {successCount}/{pointArray.Length} 个点位");
            return successCount;
        }

        /// <summary>
        /// 批量注销点位
        /// </summary>
        /// <param name="pointIds">要注销的点位ID列表</param>
        /// <returns>成功注销的点位数量</returns>
        public async Task<int> BatchUnregisterPointsAsync(IEnumerable<int> pointIds)
        {
            if (pointIds == null)
                return 0;

            var pointIdArray = pointIds.ToArray();
            var successCount = 0;

            _logger.LogInformation($"开始批量注销 {pointIdArray.Length} 个点位");

            foreach (var pointId in pointIdArray)
            {
                try
                {
                    if (_scheduledPoints.TryRemove(pointId, out var scheduledPoint))
                    {
                        RemoveFromScanRateGroup(scheduledPoint);
                        successCount++;
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"批量注销点位 {pointId} 失败");
                }
            }

            _logger.LogInformation($"批量注销完成: 成功注销 {successCount}/{pointIdArray.Length} 个点位");
            return successCount;
        }

        /// <summary>
        /// 获取调度器中的点位信息
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>调度点位信息</returns>
        public ScheduledPoint GetScheduledPoint(int pointId)
        {
            return _scheduledPoints.TryGetValue(pointId, out var point) ? point : null;
        }

        /// <summary>
        /// 获取所有调度点位信息
        /// </summary>
        /// <returns>所有调度点位信息</returns>
        public IEnumerable<ScheduledPoint> GetAllScheduledPoints()
        {
            return _scheduledPoints.Values.ToArray();
        }

        /// <summary>
        /// 获取按扫描周期分组的点位统计
        /// </summary>
        /// <returns>扫描周期分组统计</returns>
        public Dictionary<int, int> GetScanRateGroupStatistics()
        {
            var statistics = new Dictionary<int, int>();
            
            foreach (var kvp in _scanRateGroups)
            {
                if (TryParseGroupKey(kvp.Key, out var scanRate))
                {
                    if (statistics.ContainsKey(scanRate))
                    {
                        statistics[scanRate] += kvp.Value.Count;
                    }
                    else
                    {
                        statistics[scanRate] = kvp.Value.Count;
                    }
                }
            }
            
            return statistics;
        }

        /// <summary>
        /// 获取按通道分组的点位统计
        /// </summary>
        /// <returns>通道分组统计</returns>
        public Dictionary<int, int> GetChannelGroupStatistics()
        {
            var statistics = new Dictionary<int, int>();
            
            foreach (var kvp in _scanRateGroups)
            {
                if (TryParseGroupKey(kvp.Key, out var scanRate, out var channelId))
                {
                    if (statistics.ContainsKey(channelId))
                    {
                        statistics[channelId] += kvp.Value.Count;
                    }
                    else
                    {
                        statistics[channelId] = kvp.Value.Count;
                    }
                }
            }
            
            return statistics;
        }

        /// <summary>
        /// 清理所有调度点位
        /// </summary>
        public void ClearAllScheduledPoints()
        {
            _logger.LogInformation("清理所有调度点位");
            _scheduledPoints.Clear();
            _scanRateGroups.Clear();
        }

        public async Task<bool> RegisterPointAsync(DevicePoint point)
        {
            if (point == null)
                return false;

            try
            {
                // 检查点位是否已存在
                if (IsPointRegistered(point.Id))
                {
                    return true;
                }

                var scheduledPoint = new ScheduledPoint
                {
                    PointId = point.Id,
                    DeviceId = point.DeviceId,
                    ChannelId = point.Device?.ChannelId ?? 0,
                    Address = point.Address,
                    ReadWrite = point.ReadWrite,
                    DataType = point.DataType,
                    ScanRate = point.ScanRate,
                    NextExecutionTime = DateTime.UtcNow.AddMilliseconds(point.ScanRate),
                    LastExecutionTime = DateTime.MinValue,
                    ExecutionCount = 0,
                    ErrorCount = 0
                };

                _scheduledPoints[point.Id] = scheduledPoint;
                AddToScanRateGroup(scheduledPoint);

                _logger.LogInformation($"点位 {point.Id} 已注册到调度器，扫描周期: {point.ScanRate}ms");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"注册点位 {point.Id} 失败");
                return false;
            }
        }

        public async Task<bool> UnregisterPointAsync(int pointId)
        {
            try
            {
                if (_scheduledPoints.TryRemove(pointId, out var scheduledPoint))
                {
                    RemoveFromScanRateGroup(scheduledPoint);
                    _logger.LogInformation($"点位 {pointId} 已从调度器注销");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"注销点位 {pointId} 失败");
                return false;
            }
        }

        public async Task StartAsync()
        {
            lock (_lockObject)
            {
                if (_isRunning)
                    return;

                _isRunning = true;
                _timer.Change(0, _tickIntervalMs);
                _logger.LogInformation("时间轮调度器已启动");
            }
        }

        public async Task StopAsync()
        {
            lock (_lockObject)
            {
                if (!_isRunning)
                    return;

                _isRunning = false;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _logger.LogInformation("时间轮调度器已停止");
            }
        }

        public async Task<bool> UpdatePointScheduleAsync(int pointId, int newScanRate)
        {
            try
            {
                if (_scheduledPoints.TryGetValue(pointId, out var scheduledPoint))
                {
                    RemoveFromScanRateGroup(scheduledPoint);
                    scheduledPoint.ScanRate = newScanRate;
                    scheduledPoint.NextExecutionTime = DateTime.UtcNow.AddMilliseconds(newScanRate);
                    AddToScanRateGroup(scheduledPoint);
                    _logger.LogInformation($"点位 {pointId} 扫描周期已更新为 {newScanRate}ms");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新点位 {pointId} 扫描周期失败");
                return false;
            }
        }

        public SchedulerStatistics GetStatistics()
        {
            return new SchedulerStatistics
            {
                TotalScheduledTasks = _statistics.TotalScheduledTasks,
                SuccessfulTasks = _statistics.SuccessfulTasks,
                FailedTasks = _statistics.FailedTasks,
                AverageExecutionTimeMs = _statistics.AverageExecutionTimeMs,
                LastExecutionTime = _statistics.LastExecutionTime
            };
        }

        private void ProcessTimeWheel(object state)
        {
            if (!_isRunning)
                return;

            try
            {
                var currentTime = DateTime.UtcNow;

                // 检查每个扫描周期和通道组合分组
                foreach (var kvp in _scanRateGroups)
                {
                    var groupKey = kvp.Key;
                    var points = kvp.Value;

                    // 解析组合键获取扫描周期
                    if (TryParseGroupKey(groupKey, out var scanRate))
                    {
                        if (ShouldExecuteCollection(scanRate, currentTime))
                        {
                            var task = CreateCollectionTask(points, currentTime);
                            if (task != null)
                            {
                                OnCollectionTaskReady?.Invoke(task);
                                _statistics.TotalScheduledTasks++;
                                _statistics.LastExecutionTime = currentTime;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理时间轮时发生错误");
            }
        }

        private bool TryParseGroupKey(string groupKey, out int scanRate)
        {
            scanRate = 0;
            if (string.IsNullOrEmpty(groupKey))
                return false;

            var parts = groupKey.Split('_');
            if (parts.Length != 2 || !int.TryParse(parts[0], out scanRate))
                return false;

            return true;
        }

        private bool TryParseGroupKey(string groupKey, out int scanRate, out int channelId)
        {
            scanRate = 0;
            channelId = 0;
            if (string.IsNullOrEmpty(groupKey))
                return false;

            var parts = groupKey.Split('_');
            if (parts.Length != 2 || !int.TryParse(parts[0], out scanRate) || !int.TryParse(parts[1], out channelId))
                return false;

            return true;
        }

        private bool ShouldExecuteCollection(int scanRate, DateTime currentTime)
        {
            // 简单的基于时间的执行判断
            return currentTime.Ticks % (scanRate * TimeSpan.TicksPerMillisecond) < (_tickIntervalMs * TimeSpan.TicksPerMillisecond);
        }

        private CollectionTask CreateCollectionTask(List<ScheduledPoint> points, DateTime currentTime)
        {
            if (points == null || points.Count == 0)
                return null;

            // 由于现在按 (ScanRate, ChannelId) 分组，所有点位都属于同一通道
            var channelId = points.First().ChannelId;
            var deviceId = points.First().DeviceId;

            var task = new CollectionTask
            {
                ChannelId = channelId,
                DeviceId = deviceId,
                Points = points.Select(p => new CollectionPoint
                {
                    PointId = p.PointId,
                    Address = p.Address,
                    DataType = p.DataType,
                    ScanRate = p.ScanRate,
                    ReadWrite = p.ReadWrite,
                    Enable = true
                }).ToList(),
                ScheduledTime = currentTime,
                Status = Models.TaskStatus.Pending
            };

            // 更新点位的下次执行时间
            foreach (var point in points)
            {
                point.LastExecutionTime = currentTime;
                point.ExecutionCount++;
                point.NextExecutionTime = currentTime.AddMilliseconds(point.ScanRate);
            }

            return task;
        }

        private void AddToScanRateGroup(ScheduledPoint point)
        {
            var groupKey = $"{point.ScanRate}_{point.ChannelId}";
            _scanRateGroups.AddOrUpdate(
                groupKey,
                new List<ScheduledPoint> { point },
                (key, existingList) =>
                {
                    lock (existingList)
                    {
                        existingList.Add(point);
                        return existingList;
                    }
                });
        }

        private void RemoveFromScanRateGroup(ScheduledPoint point)
        {
            var groupKey = $"{point.ScanRate}_{point.ChannelId}";
            if (_scanRateGroups.TryGetValue(groupKey, out var points))
            {
                lock (points)
                {
                    points.RemoveAll(p => p.PointId == point.PointId);
                    if (points.Count == 0)
                    {
                        _scanRateGroups.TryRemove(groupKey, out _);
                    }
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }


} 