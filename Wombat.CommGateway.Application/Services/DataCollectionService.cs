using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Wombat.IndustrialCommunication.Models;
using Wombat.IndustrialCommunication.PLC;
using Wombat.IndustrialCommunication.Modbus;
using Wombat.IndustrialCommunication;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Wombat.CommGateway.Infrastructure.Repositories;
using Wombat.Extensions.DataTypeExtensions;
using Wombat.CommGateway.Domain.Enums;
using System.Diagnostics;
using System.Linq;
using Wombat.CommGateway.Application.Services.DataCollection;
using Wombat.CommGateway.Application.Services.DataCollection.Models;




namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 数据采集服务实现
    /// 基于KepServer风格的多周期采样系统
    /// 支持动态点位管理
    /// </summary>
    /// 
    [AutoInject<IDataCollectionService>(ServiceLifetime.Singleton)]
    [AutoInject<IPointChangeNotificationService>(ServiceLifetime.Singleton)]
    public class DataCollectionService : BackgroundService, IDataCollectionService, IPointChangeNotificationService
    {
        private readonly ILogger<DataCollectionService> _logger;
        private readonly Dictionary<int, bool> _collectionStatus = new();
        private readonly Dictionary<int, string> _collectionErrors = new();
        private CancellationTokenSource _cancellationTokenSource;
        private readonly CacheManager _cacheManager;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly TimeWheelScheduler _timeWheelScheduler;
        private readonly ConnectionPoolManager _connectionPoolManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private TaskCompletionSource<bool> _serviceCompletionSource;
        private volatile bool _isServiceRunning;
        private readonly object _serviceLock = new object();


        
        /// <summary>
        /// 构造函数
        /// </summary>
        public DataCollectionService(
            ILogger<DataCollectionService> logger,
            TimeWheelScheduler timeWheelScheduler,
            CacheManager cacheManager,
            ConnectionPoolManager connectionPoolManager,
            ISubscriptionManager subscriptionManager,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _timeWheelScheduler = timeWheelScheduler ?? throw new ArgumentNullException(nameof(timeWheelScheduler));
            _cacheManager = cacheManager??throw new ArgumentNullException(nameof(cacheManager));
            _connectionPoolManager = connectionPoolManager ?? throw new ArgumentNullException(nameof(connectionPoolManager));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("KepServer风格数据采集服务启动中...");
            
            try
            {
                // 初始化所有组件
                await InitializeComponentsAsync();
                
                // 注册所有点位到调度器
                await RegisterAllPointsAsync();
                
                // 启动调度器
                await StartSchedulerAsync();
                
                // 启动缓存管理器
                StartCacheManager();
                
                lock (_serviceLock)
                {
                    _isServiceRunning = true;
                }
                
                _logger.LogInformation("数据采集服务已启动 - 调度器状态: {SchedulerRunning}, 服务状态: {ServiceRunning}", 
                    _timeWheelScheduler.IsRunning, _isServiceRunning);
                
                // 调用基类方法
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据采集服务启动时发生错误");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("数据采集服务停止中...");
            
            try
            {
                // 停止调度器
                if (_timeWheelScheduler.IsRunning)
                {
                    await StopSchedulerAsync();
                }
                
                // 停止缓存管理器
                StopCacheManager();
                
                lock (_serviceLock)
                {
                    _isServiceRunning = false;
                }
                
                _logger.LogInformation("数据采集服务已停止 - 调度器状态: {SchedulerRunning}, 服务状态: {ServiceRunning}", 
                    _timeWheelScheduler.IsRunning, _isServiceRunning);
                
                // 调用基类方法
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据采集服务停止时发生错误");
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 初始化 TaskCompletionSource
            _serviceCompletionSource = new TaskCompletionSource<bool>();
            
            // 注册取消令牌回调
            stoppingToken.Register(() => _serviceCompletionSource.TrySetResult(true));
            
            try
            {
                // 等待取消信号 - 更优雅的方式
                await _serviceCompletionSource.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据采集服务执行过程中发生错误");
            }
            finally
            {
                // 确保 TaskCompletionSource 被设置
                _serviceCompletionSource?.TrySetResult(true);
            }
        }
        
        private async Task InitializeComponentsAsync()
        {
            _logger.LogInformation("初始化数据采集组件...");
            _timeWheelScheduler.OnCollectionTaskReady += HandleCollectionTaskAsync;
           _cacheManager.OnFlushRequired += FlushCacheToDatabase;
            
            // 记录层级关系维护方式的变更
            _logger.LogInformation("层级关系维护已集成到数据采集服务中，将在点位注册时同步更新层级关系");
        }
        
        private async Task StartSchedulerAsync()
        {
            await _timeWheelScheduler.StartAsync();

        }

        private async Task StopSchedulerAsync()
        {
            await _timeWheelScheduler.StopAsync();

        }

        private void StartCacheManager()
        {
          _cacheManager.Start();

        }

        private void StopCacheManager()
        {
            _cacheManager.Stop();

        }

        private async Task RegisterAllPointsAsync()
        {
            _logger.LogInformation("注册所有点位到调度器并更新层级关系...");
            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                try
                {
                    var devicePointRepository = scope.ServiceProvider.GetRequiredService<IDevicePointRepository>();
                    var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    
                    // 获取所有启用的设备和点位
                    var devices = await deviceRepository.GetAllAsync();
                    var enabledDevices = devices.Where(d => d.Enable).ToList();
                    
                    int registeredCount = 0;
                    
                    // 清空层级关系缓存，准备重建
                    _logger.LogInformation("清空层级关系缓存，准备重建...");
                    
                    // 首先建立设备与设备组的层级关系
                    foreach (var device in devices)
                    {
                        _subscriptionManager.UpdateDeviceHierarchy(device.Id, device.DeviceGroupId);
                    }
                    
                    _logger.LogInformation($"已更新 {devices.Count} 个设备的层级关系");
                    
                    // 然后处理启用的设备和点位
                    foreach (var device in enabledDevices)
                    {
                        if (device.Points != null)
                        {
                            foreach (var point in device.Points)
                            {
                                // 更新点位与设备的层级关系
                                _subscriptionManager.UpdatePointHierarchy(point.Id, device.Id);
                                
                                if (point.Enable)
                                {
                                    // 确保点位有正确的设备导航属性，这样调度器就能获取到ChannelId
                                    if (point.Device == null)
                                    {
                                        point.Device = device;
                                    }
                                    
                                    await _timeWheelScheduler.RegisterPointAsync(point);
                                    registeredCount++;
                                    
                                    _logger.LogDebug($"点位 {point.Id} 已注册到调度器，设备 {device.Id}，通道 {device.ChannelId}");
                                }
                            }
                        }
                    }
                    
                    _logger.LogInformation($"成功注册 {registeredCount} 个点位到调度器，并更新了层级关系缓存");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "注册点位到调度器并更新层级关系时发生错误");
                    throw;
                }
            }
        }
        
        /// <summary>
        /// 采集任务
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private async Task HandleCollectionTaskAsync(CollectionTask task)
        {
            try
            {
                _logger.LogDebug($"开始处理采集任务 - 通道ID: {task.ChannelId}, 点位数量: {task.Points?.Count ?? 0}");
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
                    
                    var channel = await channelRepository.GetByIdAsync(task.ChannelId);
                    
                    if (channel == null || !channel.Enable)
                    {
                        _logger.LogWarning($"通道 {task.ChannelId} 不存在或未启用，跳过采集任务");
                        return;
                    }
                    
                    _logger.LogDebug($"通道 {task.ChannelId} 有效，开始获取连接");
                    
                    // 从连接池获取连接
                    var client = await _connectionPoolManager.GetConnectionAsync(channel);
                    if (client == null || !client.Connected)
                    {
                        _logger.LogWarning($"无法连接到通道 {task.ChannelId}，跳过采集任务");
                        return;
                    }
                    
                    _logger.LogDebug($"通道 {task.ChannelId} 连接成功，开始准备采集地址");
                    
                    try
                    {
                        // 准备批量读取的地址
                        Dictionary<string, DataTypeEnums> addresses = new Dictionary<string, DataTypeEnums>();
                        Dictionary<int, string> pointAddresses = new Dictionary<int, string>();
                        
                        foreach (var point in task.Points)
                        {
                            if (point.Enable && (point.ReadWrite == ReadWriteType.Read || point.ReadWrite == ReadWriteType.ReadWrite))
                            {
                                var address = point.Address.ToUpper();
                                if (!addresses.ContainsKey(address))
                                {
                                    addresses.Add(address, ToDataTypeEnums(point.DataType));
                                }
                                pointAddresses[point.PointId] = address;
                            }

                        }
                        
                        _logger.LogInformation($"通道 {task.ChannelId} 准备读取 {addresses.Count} 个地址，映射到 {pointAddresses.Count} 个点位");
                        
                        // 批量读取数据
                        var result = await client.BatchReadAsync(addresses);
                        if (result.IsSuccess)
                        {
                            _logger.LogInformation($"通道 {task.ChannelId} 批量读取成功，共 {addresses.Count} 个地址");
                            
                            // 处理读取结果
                            Dictionary<int, (string Value, DataPointStatus Status)> updates = new Dictionary<int, (string, DataPointStatus)>();
                            
                            foreach (var (pointId, address) in pointAddresses)
                            {
                                if (result.ResultValue.TryGetValue(address, out var tuple))
                                {
                                    var (dataType, rawValue) = tuple;
                                    var value = rawValue?.ToString() ?? string.Empty;
                                    updates[pointId] = (value, DataPointStatus.Good);
                                    
                                    _logger.LogInformation($"点位 {pointId} (地址: {address}) 读取成功: {value}");
                                }
                                else
                                {
                                    updates[pointId] = (string.Empty, DataPointStatus.Bad);
                                    _logger.LogWarning($"点位 {pointId} (地址: {address}) 读取失败 - 未在结果中找到");
                                }
                            }
                            
                            _logger.LogInformation($"通道 {task.ChannelId} 开始更新缓存，共 {updates.Count} 个点位更新");
                            
                            // 更新缓存 - 使用强制通知确保推送
                            _cacheManager.BatchUpdateCache(updates, forceNotify: true);
                            _logger.LogInformation($"通道 {task.ChannelId} 采集完成，已更新 {updates.Count} 个点位的缓存并强制推送");
                            
                        }
                        else
                        {
                            _logger.LogWarning($"通道 {task.ChannelId} 批量读取失败: {result.Message}");
                            
                            // 更新点位状态为错误
                            Dictionary<int, (string Value, DataPointStatus Status)> updates = new Dictionary<int, (string, DataPointStatus)>();
                            foreach (var point in task.Points)
                            {
                                updates[point.PointId] = (string.Empty, DataPointStatus.Bad);
                            }
                            
                            _logger.LogInformation($"通道 {task.ChannelId} 将 {updates.Count} 个点位状态设为错误并强制推送");
                          _cacheManager.BatchUpdateCache(updates, forceNotify: true);
                        }
                    }
                    finally
                    {
                        // 释放连接回连接池
                        _connectionPoolManager.ReleaseConnection(task.ChannelId, client);
                        _logger.LogDebug($"通道 {task.ChannelId} 连接已释放回连接池");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理采集任务时发生错误: 通道ID={task.ChannelId}");
            }
        }

        /// <inheritdoc/>
        public async Task WriteDataAsync(int pointId, object value)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var devicePointRepository = scope.ServiceProvider.GetRequiredService<IDevicePointRepository>();
                    var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();

                    var point = await devicePointRepository.GetDevicePointAsync(pointId);

                    if (point == null)
                    {
                        throw new ArgumentException($"Point with id {pointId} not found.");
                    }
                    if (!point.Enable && (point.ReadWrite != ReadWriteType.Write || point.ReadWrite != ReadWriteType.ReadWrite))
                    {
                        throw new ArgumentException($"Point type is Error{point.ReadWrite}");

                    }
                    var device = await deviceRepository.GetByIdAsync(point.DeviceId);

                    if (device == null || !device.Enable)
                    {
                        throw new InvalidOperationException($"Device {point.DeviceId} not found or not enabled.");
                    }

                    var channel = await channelRepository.GetByIdAsync(device.ChannelId);

                    if (channel == null || !channel.Enable)
                    {
                        throw new InvalidOperationException($"Channel {device.ChannelId} not found or not enabled.");
                    }

                    // 从连接池获取连接
                    var client = await _connectionPoolManager.GetConnectionAsync(channel);
                    if (client == null || !client.Connected)
                    {
                        throw new InvalidOperationException($"Cannot connect to channel {channel.Id}.");
                    }

                    try
                    {
                        // 写入数据
                        var dataTypeEnum = ToDataTypeEnums(point.DataType);
                        var result = await client.WriteAsync(dataTypeEnum, point.Address, value);
                        if (!result.IsSuccess)
                        {
                            throw new InvalidOperationException($"Write operation failed: {result.Message}");
                        }

                        // 更新缓存
                        _cacheManager.UpdateCache(pointId, value.ToString(), DataPointStatus.Good);
                    }
                    finally
                    {
                        // 释放连接回连接池
                        _connectionPoolManager.ReleaseConnection(channel.Id, client);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing data for point {pointId}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task BatchWriteDataAsync(Dictionary<int, object> pointValues)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var devicePointRepository = scope.ServiceProvider.GetRequiredService<IDevicePointRepository>();
                    var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
                    var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();

                    // 按设备分组
                    var pointsByDevice = new Dictionary<int, List<(int PointId, object Value)>>();
                    foreach (var kvp in pointValues)
                    {
                        var point = await devicePointRepository.GetDevicePointAsync(kvp.Key);
                        if (point != null && point.Enable && (point.ReadWrite == ReadWriteType.Write || point.ReadWrite == ReadWriteType.ReadWrite))
                        {
                            if (!pointsByDevice.ContainsKey(point.DeviceId))
                            {
                                pointsByDevice[point.DeviceId] = new List<(int, object)>();
                            }
                            pointsByDevice[point.DeviceId].Add((kvp.Key, kvp.Value));
                        }
                    }

                    // 按设备处理
                    foreach (var deviceKvp in pointsByDevice)
                    {
                        var deviceId = deviceKvp.Key;
                        var devicePoints = deviceKvp.Value;

                        var device = await deviceRepository.GetByIdAsync(deviceId);
                        if (device == null || !device.Enable)
                        {
                            continue;
                        }

                        var channel = await channelRepository.GetByIdAsync(device.ChannelId);
                        if (channel == null || !channel.Enable)
                        {
                            continue;
                        }

                        // 从连接池获取连接
                        var client = await _connectionPoolManager.GetConnectionAsync(channel);
                        if (client == null || !client.Connected)
                        {
                            continue;
                        }

                        try
                        {
                            // 按点位处理
                            foreach (var (pointId, value) in devicePoints)
                            {
                                var point = await devicePointRepository.GetDevicePointAsync(pointId);

                                // 写入数据
                                var dataTypeEnum = ToDataTypeEnums(point.DataType);
                                var result = await client.WriteAsync(dataTypeEnum, point.Address, value);
                                if (result.IsSuccess)
                                {
                                    // 更新缓存
                                    _cacheManager.UpdateCache(pointId, value.ToString(), DataPointStatus.Good);
                                }
                                else
                                {
                                    _logger.LogWarning($"Write operation failed for point {pointId}: {result.Message}");
                                }
                            }
                        }
                        finally
                        {
                            // 释放连接回连接池
                            _connectionPoolManager.ReleaseConnection(channel.Id, client);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch writing data");
                throw;
            }
        }


        private void FlushCacheToDatabase(Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)> dirtyData)
        {
            if (dirtyData == null || dirtyData.Count == 0)
                return;
                
            try
            {
                //using (var scope = _serviceScopeFactory.CreateScope())
                //{
                //    var devicePointRepository = scope.ServiceProvider.GetRequiredService<IDevicePointRepository>();
                    
                //    // 批量更新数据库
                //    foreach (var batch in BatchPoints(dirtyData, 100)) // 每批100个点位
                //    {
                //        Task.Run(async () =>
                //        {
                //            try
                //            {
                //                foreach (var (pointId, (value, status, updateTime)) in batch)
                //                {
                //                    await devicePointRepository.Select.Where(x => x.Id == pointId).ToUpdate()
                //                        .Set(x => x.Value, value)
                //                        .Set(x => x.Status, status)
                //                        .Set(x => x.UpdateTime, updateTime)
                //                        .ExecuteAffrowsAsync();
                //                }
                                
                //                // 标记为已刷新
                //                _cacheManager.MarkAsFlushed(batch.Keys);
                                
                //            }
                //            catch (Exception ex)
                //            {
                //                _logger.LogError(ex, "批量更新数据库时发生错误");
                //            }
                //        });
                //    }
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新缓存到数据库时发生错误");
            }
        }
        
        private IEnumerable<Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)>> BatchPoints(
            Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)> points, int batchSize)
        {
            var batch = new Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)>();
            int count = 0;
            
            foreach (var kvp in points)
            {
                batch.Add(kvp.Key, kvp.Value);
                count++;
                
                if (count >= batchSize)
                {
                    yield return batch;
                    batch = new Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)>();
                    count = 0;
                }
            }
            
            if (count > 0)
            {
                yield return batch;
            }
        }


        /// <inheritdoc/>
        public async Task StartCollectionAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation($"Starting data collection for device {deviceId}");
                _collectionStatus[deviceId] = true;
                _collectionErrors[deviceId] = null;

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var devicePointRepository = scope.ServiceProvider.GetRequiredService<IDevicePointRepository>();
                    var points = await devicePointRepository.GetDevicePointsAsync(deviceId);
                    
                    // 注册设备的所有点位到调度器
                    foreach (var point in points)
                    {
                        if (point.Enable)
                        {
                            await _timeWheelScheduler.RegisterPointAsync(point);
                        }
                    }
                }

                _logger.LogInformation($"Data collection started successfully for device {deviceId}");
            }
            catch (Exception ex)
            {
                _collectionStatus[deviceId] = false;
                _collectionErrors[deviceId] = ex.Message;
                _logger.LogError(ex, $"Error starting data collection for device {deviceId}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task StopCollectionAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation($"Stopping data collection for device {deviceId}");
                _collectionStatus[deviceId] = false;

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var devicePointRepository = scope.ServiceProvider.GetRequiredService<IDevicePointRepository>();
                    var points = await devicePointRepository.GetDevicePointsAsync(deviceId);
                    
                    // 从调度器注销设备的所有点位
                    foreach (var point in points)
                    {
                        await _timeWheelScheduler.UnregisterPointAsync(point.Id);
                    }
                }

                _logger.LogInformation($"Data collection stopped successfully for device {deviceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping data collection for device {deviceId}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DataCollectionRecord>> GetCollectionDataAsync(int deviceId, DateTime startTime, DateTime endTime)
        {
            try
            {
                //return await _dataCollectionRecordService.GetCollectionDataAsync(deviceId, startTime, endTime);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting collection data for device {deviceId}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<int, object>> GetRealtimeDataAsync(int deviceId)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var devicePointRepository = scope.ServiceProvider.GetRequiredService<IDevicePointRepository>();
                    var points = await devicePointRepository.GetDevicePointsAsync(deviceId);
                    
                    var result = new Dictionary<int, object>();
                    var pointIds = points.Select(p => p.Id).ToList();
                    
                    // 从缓存获取最新数据
                    var cachedValues = _cacheManager.BatchGetCachedValues(pointIds);
                    foreach (var point in points)
                    {
                        if (cachedValues.TryGetValue(point.Id, out var cachedValue))
                        {
                            result[point.Id] = cachedValue.Value;
                        }
                        else
                        {
                            result[point.Id] = point.Value;
                        }
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting realtime data for device {deviceId}");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<bool> GetCollectionStatusAsync(int deviceId)
        {
            return Task.FromResult(_collectionStatus.TryGetValue(deviceId, out var status) && status);
        }

        /// <inheritdoc/>
        public Task<string> GetCollectionErrorAsync(int deviceId)
        {
            return Task.FromResult(_collectionErrors.TryGetValue(deviceId, out var error) ? error : null);
        }

        /// <inheritdoc/>
        public async Task RestartServiceAsync()
        {
            try
            {
                _logger.LogInformation("重启数据采集服务...");
                
                // 先停止服务
                await StopAsync(CancellationToken.None);
                
                // 等待一小段时间确保完全停止
                await Task.Delay(1000);
                
                // 清理连接池
                await _connectionPoolManager.ClearAllPoolsAsync();
                
                // 清理时间轮调度器的所有点位
                _timeWheelScheduler.ClearAllScheduledPoints();
                
                // 清理缓存管理器的所有数据
                _cacheManager.ClearAllCache();
                
                // 再启动服务
                await StartAsync(CancellationToken.None);
                
                _logger.LogInformation("数据采集服务重启完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重启数据采集服务时发生错误");
                throw;
            }
        }



        /// <inheritdoc/>
        public Task<bool> GetServiceStatusAsync()
        {
            return Task.FromResult(_isServiceRunning);
        }

        public DataTypeEnums ToDataTypeEnums(DataType dataType)
        {
            // 使用 Enum.TryParse 保证安全转换
            if (Enum.TryParse<DataTypeEnums>(dataType.ToString(), out var result))
            {
                return result;
            }
            return DataTypeEnums.None;
        }

        public DataType ToDataType(DataTypeEnums dataTypeEnum)
        {
            if (Enum.TryParse<DataType>(dataTypeEnum.ToString(), out var result))
            {
                return result;
            }
            return DataType.None;
        }

        #region IPointChangeNotificationService Implementation

        /// <inheritdoc/>
        public async Task OnPointCreatedAsync(DevicePoint point)
        {
            try
            {
                _logger.LogInformation($"收到点位创建通知: PointId={point.Id}, Name={point.Name}, Enable={point.Enable}");
                
                // 检查点位是否启用以及所属设备是否启用
                if (!point.Enable)
                {
                    return;
                }

                // 获取设备信息以检查设备是否启用
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    var device = await deviceRepository.GetByIdAsync(point.DeviceId);
                    
                    if (device == null || !device.Enable)
                    {
                        return;
                    }
                    
                    // 更新点位与设备的层级关系
                    _subscriptionManager.UpdatePointHierarchy(point.Id, point.DeviceId);
                    _logger.LogDebug($"已更新点位 {point.Id} 与设备 {point.DeviceId} 的层级关系");
                    
                    // 确保点位有正确的设备导航属性，这样调度器就能获取到ChannelId
                    if (point.Device == null)
                    {
                        point.Device = device;
                    }
                }

                // 注册点位到调度器
                var registered = await _timeWheelScheduler.RegisterPointAsync(point);
                if (registered)
                {
                    _logger.LogInformation($"点位 {point.Id} 已成功注册到调度器，通道 {point.Device?.ChannelId}");
                }
                else
                {
                    _logger.LogWarning($"点位 {point.Id} 注册到调度器失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理点位创建通知时发生错误: PointId={point.Id}");
            }
        }

        /// <inheritdoc/>
        public async Task OnPointUpdatedAsync(DevicePoint point, int? oldScanRate = null, bool? oldEnable = null)
        {
            try
            {
                _logger.LogInformation($"收到点位更新通知: PointId={point.Id}, Name={point.Name}, Enable={point.Enable}");
                
                // 处理启用状态变更
                if (oldEnable.HasValue && oldEnable.Value != point.Enable)
                {
                    if (point.Enable)
                    {
                        // 启用点位：注册到调度器
                        await OnPointEnabledChangedAsync(point.Id, true);
                    }
                    else
                    {
                        // 禁用点位：从调度器注销
                        await OnPointEnabledChangedAsync(point.Id, false);
                    }
                }
                
                // 处理扫描周期变更
                if (oldScanRate.HasValue && oldScanRate.Value != point.ScanRate && point.Enable)
                {
                    var updated = await _timeWheelScheduler.UpdatePointScheduleAsync(point.Id, point.ScanRate);
                    if (updated)
                    {
                        _logger.LogInformation($"点位 {point.Id} 扫描周期已更新: {oldScanRate} -> {point.ScanRate}");
                    }
                    else
                    {
                        _logger.LogWarning($"点位 {point.Id} 扫描周期更新失败");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理点位更新通知时发生错误: PointId={point.Id}");
            }
        }

        /// <inheritdoc/>
        public async Task OnPointDeletedAsync(int pointId)
        {
            try
            {
                _logger.LogInformation($"收到点位删除通知: PointId={pointId}");
                
                // 从调度器注销点位
                var unregistered = await _timeWheelScheduler.UnregisterPointAsync(pointId);
                if (unregistered)
                {
                    _logger.LogInformation($"点位 {pointId} 已从调度器注销");
                }
                else
                {
                    _logger.LogInformation($"点位 {pointId} 在调度器中不存在或注销失败");
                }

                // 从层级关系缓存中移除点位
                _subscriptionManager.RemovePointHierarchy(pointId);
                _logger.LogDebug($"已从层级关系缓存中移除点位 {pointId}");

                // 清理缓存中的点位数据
                _cacheManager.RemoveFromCache(pointId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理点位删除通知时发生错误: PointId={pointId}");
            }
        }

        /// <inheritdoc/>
        public async Task OnPointEnabledChangedAsync(int pointId, bool enabled)
        {
            try
            {
                _logger.LogInformation($"收到点位启用状态变更通知: PointId={pointId}, Enabled={enabled}");
                
                if (enabled)
                {
                    // 启用点位：从数据库重新加载点位信息并注册到调度器
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var devicePointRepository = scope.ServiceProvider.GetRequiredService<IDevicePointRepository>();
                        var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                        
                        var point = await devicePointRepository.GetDevicePointAsync(pointId);
                        if (point != null)
                        {
                            // 检查设备是否启用
                            var device = await deviceRepository.GetByIdAsync(point.DeviceId);
                            if (device != null && device.Enable)
                            {
                                // 确保点位有正确的设备导航属性，这样调度器就能获取到ChannelId
                                if (point.Device == null)
                                {
                                    point.Device = device;
                                }
                                
                                var registered = await _timeWheelScheduler.RegisterPointAsync(point);
                                if (registered)
                                {
                                    _logger.LogInformation($"点位 {pointId} 已启用并注册到调度器，通道 {device.ChannelId}");
                                }
                                else
                                {
                                    _logger.LogWarning($"点位 {pointId} 注册到调度器失败");
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"点位 {pointId} 所属设备未启用，跳过注册");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"点位 {pointId} 不存在，无法启用");
                        }
                    }
                }
                else
                {
                    // 禁用点位：从调度器注销
                    var unregistered = await _timeWheelScheduler.UnregisterPointAsync(pointId);
                    if (unregistered)
                    {
                        _logger.LogInformation($"点位 {pointId} 已禁用并从调度器注销");
                    }
                    else
                    {
                        _logger.LogInformation($"点位 {pointId} 在调度器中不存在或注销失败");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理点位启用状态变更通知时发生错误: PointId={pointId}");
            }
        }

        /// <inheritdoc/>
        public async Task OnPointsBatchImportedAsync(DevicePoint[] points)
        {
            try
            {
                _logger.LogInformation($"收到批量点位导入通知: 共 {points.Length} 个点位");
                
                var registeredCount = 0;
                var hierarchyUpdateCount = 0;
                
                foreach (var point in points)
                {
                    // 更新点位与设备的层级关系
                    _subscriptionManager.UpdatePointHierarchy(point.Id, point.DeviceId);
                    hierarchyUpdateCount++;
                    
                    if (point.Enable)
                    {
                        // 检查设备是否启用
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                            var device = await deviceRepository.GetByIdAsync(point.DeviceId);
                            
                            if (device != null && device.Enable)
                            {
                                // 确保点位有正确的设备导航属性，这样调度器就能获取到ChannelId
                                if (point.Device == null)
                                {
                                    point.Device = device;
                                }
                                
                                var registered = await _timeWheelScheduler.RegisterPointAsync(point);
                                if (registered)
                                {
                                    registeredCount++;
                                }
                            }
                        }
                    }
                }
                
                _logger.LogInformation($"批量导入完成: 成功注册 {registeredCount} 个点位到调度器，更新 {hierarchyUpdateCount} 个点位的层级关系");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理批量点位导入通知时发生错误");
            }
        }

        #endregion
    }
} 