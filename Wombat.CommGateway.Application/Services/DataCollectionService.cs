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


namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 数据采集服务实现
    /// </summary>
    /// 
    [AutoInject(typeof(IDataCollectionService),serviceLifetime:ServiceLifetime.Singleton)]
    public class DataCollectionService : BackgroundService, IDataCollectionService
    {
        private readonly ILogger<DataCollectionService> _logger;
        private readonly IDevicePointRepository _devicePointRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IDataCollectionRecordService _dataCollectionRecordService;
        private readonly IChannelRepository _channelRepository;
        private readonly Dictionary<int, bool> _collectionStatus = new();
        private readonly Dictionary<int, string> _collectionErrors = new();
        private CancellationTokenSource _cancellationTokenSource;
        private IServiceScopeFactory _serviceScopeFactory;
        /// <summary>
        /// 构造函数
        /// </summary>
        public DataCollectionService(
            ILogger<DataCollectionService> logger,
            IDevicePointRepository devicePointRepository,
            IDataCollectionRecordService dataCollectionRecordService,
            IDeviceRepository deviceRepository,
            IServiceScopeFactory serviceScopeFactory,
            IChannelRepository channelRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _devicePointRepository = devicePointRepository ?? throw new ArgumentNullException(nameof(devicePointRepository));
            _dataCollectionRecordService = dataCollectionRecordService ?? throw new ArgumentNullException(nameof(dataCollectionRecordService));
            _channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
            _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Modbus TCP Background Service starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var channels = await _channelRepository.GetAllAsync();
                    var devices = await _deviceRepository.GetAllAsync();
                    var dataPoints = await _devicePointRepository.GetAllAsync();
                    foreach (var channel in channels)
                    {
                        if (channel.Enable)
                        {

                        }

                    }
                }
            }

        }



        //public IDeviceClient CreatClientByChannel(ChannelDto channelDto)
        //{
        //    switch (channel.)
        //    {
        //        default:
        //            break;
        //    }
        //}

        /// <inheritdoc/>
        public async Task StartCollectionAsync(int deviceId)
        {
            //try
            //{
            //    _logger.LogInformation($"Starting data collection for device {deviceId}");
            //    _collectionStatus[deviceId] = true;
            //    _collectionErrors[deviceId] = null;

            //    // 获取设备的所有点位
            //    var points = await _devicePointService.GetDevicePointsAsync(deviceId);
                
            //    // 启动通信通道
            //    await _channelService.StartAsync(deviceId);

            //    _logger.LogInformation($"Data collection started successfully for device {deviceId}");
            //}
            //catch (Exception ex)
            //{
            //    _collectionStatus[deviceId] = false;
            //    _collectionErrors[deviceId] = ex.Message;
            //    _logger.LogError(ex, $"Error starting data collection for device {deviceId}");
            //    throw;
            //}
             await Task.CompletedTask;

        }

        /// <inheritdoc/>
        public async Task StopCollectionAsync(int deviceId)
        {
            //try
            //{
            //    _logger.LogInformation($"Stopping data collection for device {deviceId}");
            //    _collectionStatus[deviceId] = false;

            //    // 停止通信通道
            //    await _channelService.StopAsync(deviceId);

            //    _logger.LogInformation($"Data collection stopped successfully for device {deviceId}");
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, $"Error stopping data collection for device {deviceId}");
            //    throw;
            //}
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
            //try
            //{
            //    return await _channelService.GetRealtimeDataAsync(deviceId);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, $"Error getting realtime data for device {deviceId}");
            //    throw;
            //}

            return null;
        }

        /// <inheritdoc/>
        public async Task WriteDataAsync(int pointId, object value)
        {
            try
            {

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