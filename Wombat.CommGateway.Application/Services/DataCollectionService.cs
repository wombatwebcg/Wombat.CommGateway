using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;


namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 数据采集服务实现
    /// </summary>
    /// 
    [AutoInject(typeof(IDataCollectionService))]
    public class DataCollectionService : IDataCollectionService
    {
        private readonly ILogger<DataCollectionService> _logger;
        private readonly IDevicePointService _devicePointService;
        private readonly IDataCollectionRecordService _dataCollectionRecordService;
        private readonly IChannelService _communicationChannelService;
        private readonly Dictionary<int, bool> _collectionStatus = new();
        private readonly Dictionary<int, string> _collectionErrors = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        public DataCollectionService(
            ILogger<DataCollectionService> logger,
            IDevicePointService devicePointService,
            IDataCollectionRecordService dataCollectionRecordService,
            IChannelService communicationChannelService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _devicePointService = devicePointService ?? throw new ArgumentNullException(nameof(devicePointService));
            _dataCollectionRecordService = dataCollectionRecordService ?? throw new ArgumentNullException(nameof(dataCollectionRecordService));
            _communicationChannelService = communicationChannelService ?? throw new ArgumentNullException(nameof(communicationChannelService));
        }

        /// <inheritdoc/>
        public async Task StartCollectionAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation($"Starting data collection for device {deviceId}");
                _collectionStatus[deviceId] = true;
                _collectionErrors[deviceId] = null;

                // 获取设备的所有点位
                var points = await _devicePointService.GetDevicePointsAsync(deviceId);
                
                // 启动通信通道
                await _communicationChannelService.StartAsync(deviceId);

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

                // 停止通信通道
                await _communicationChannelService.StopAsync(deviceId);

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
                return await _dataCollectionRecordService.GetCollectionDataAsync(deviceId, startTime, endTime);
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
                return await _communicationChannelService.GetRealtimeDataAsync(deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting realtime data for device {deviceId}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task WriteDataAsync(int pointId, object value)
        {
            try
            {
                await _communicationChannelService.WriteDataAsync(pointId, value);
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
                await _communicationChannelService.BatchWriteDataAsync(pointValues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch writing data");
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
    }
} 