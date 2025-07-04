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


namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 数据采集服务实现
    /// </summary>
    /// 
    [AutoInject(typeof(IDataCollectionService),serviceLifetime:ServiceLifetime.Singleton)]
    public class DataCollectionService : BackgroundService, IDataCollectionService
    {
        private  ILogger<DataCollectionService> _logger;

        private readonly Dictionary<int, bool> _collectionStatus = new();
        private readonly Dictionary<int, string> _collectionErrors = new();
        private CancellationTokenSource _cancellationTokenSource;
        private IServiceScopeFactory _serviceScopeFactory;
        private static readonly Dictionary<string, SiemensVersion> CpuTypeToVersion = new()
        {
            { "S7-200Smart", SiemensVersion.S7_200Smart },
            { "S7-200", SiemensVersion.S7_200 },
            { "S7-300", SiemensVersion.S7_300 },
            { "S7-400", SiemensVersion.S7_400 },
            { "S7-1200", SiemensVersion.S7_1200 },
            { "S7-1500", SiemensVersion.S7_1500 }
        };
        /// <summary>
        /// 构造函数
        /// </summary>
        public DataCollectionService(
            ILogger<DataCollectionService> logger,
            IServiceScopeFactory serviceScopeFactory
)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Modbus TCP Background Service starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
                    var devicePointRepository = scope.ServiceProvider.GetRequiredService<IDevicePointRepository>();
                    //var dataPoints = await devicePointRepository.GetAllAsync();
                    var channels = await channelRepository.GetAllAsync();
                    var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    var devices = await deviceRepository.GetAllAsync();
                    List<Task> tasks = new List<Task>();
                    foreach (var channel in channels)
                    {
                        if (channel.Enable)
                        {
                            Task task = Task.Run(async () =>
                            {
                                var client = CreatClientByChannel(channel);
                                await client.ConnectAsync();
                                if (client.Connected)
                                {
                                    Dictionary<string, DataTypeEnums> addresses = new Dictionary<string, DataTypeEnums>();
                                    Dictionary<int, string> values = new Dictionary<int, string>();
                                    Dictionary<int, string> valueAddresss = new Dictionary<int, string>();

                                    foreach (var device in devices)
                                    {
                                        if (device.Enable && device.ChannelId == channel.Id)
                                        {
                                            foreach (var point in device.Points)
                                            {
                                                if (point.Enable)
                                                {
                                                    var address = point.Address.ToUpper();
                                                    if (!addresses.ContainsKey(address))
                                                    {
                                                        addresses.Add(address, ToDataTypeEnums(point.DataType));
                                                    }
                                                    values.Add(point.Id, "");
                                                    valueAddresss.Add(point.Id, address);
                                                }
                                            }

                                            var result = await client.BatchReadAsync(addresses);
                                            if (result.IsSuccess)
                                            {
                                                foreach (var (id, addr) in valueAddresss)
                                                {
                                                    if (result.ResultValue.TryGetValue(addr, out var tuple))
                                                    {
                                                        var (dataType, rawValue) = tuple;
                                                        values[id] = rawValue?.ToString() ?? string.Empty;
                                                    }
                                                }

                                                foreach (var (id, data) in values)
                                                {

                                                    await devicePointRepository.Select.Where(x => x.Id == id).ToUpdate()
                                                      .Set(x => x.Value, data)
                                                      .Set(x => x.UpdateTime, DateTime.Now).Set(x => x.Status, DataPointStatus.Good)
                                                      .ExecuteAffrowsAsync();

                                                }
                                            }
                                        }
                                    }
                                    await client.DisconnectAsync();
                                }
                            });
                            tasks.Add(task);
                        }

                    }
                    await Task.WhenAll(tasks);
                }
                await Task.Delay(10000);
            }

        }



        public IDeviceClient CreatClientByChannel(Channel channel)
        {
            if (channel == null || channel.Configuration == null)
                return null;

            try
            {
                switch (channel.Protocol)
                {
                    case ProtocolType.SiemensS7:
                        // 提取参数
                        var ip = channel.Configuration.ContainsKey("ipAddress") ? channel.Configuration["ipAddress"] : null;
                        var portStr = channel.Configuration.ContainsKey("port") ? channel.Configuration["port"] : "102";
                        var cpuType = channel.Configuration.ContainsKey("cpuType") ? channel.Configuration["cpuType"] : "S7-1200";
                        var rackStr = channel.Configuration.ContainsKey("rack") ? channel.Configuration["rack"] : "0";
                        var slotStr = channel.Configuration.ContainsKey("slot") ? channel.Configuration["slot"] : "0";

                        if (string.IsNullOrWhiteSpace(ip))
                            throw new ArgumentException("SiemensS7配置缺少ipAddress参数");

                        if (!int.TryParse(portStr, out int port))
                            port = 102;
                        if (!byte.TryParse(rackStr, out byte rack))
                            rack = 0;
                        if (!byte.TryParse(slotStr, out byte slot))
                            slot = 1;

                        if (!CpuTypeToVersion.TryGetValue(cpuType, out SiemensVersion version))
                            throw new ArgumentException($"未知的cpuType: {cpuType}");

                        // 实例化SiemensClient
                        return new SiemensClient(ip, port, version, rack, slot);

                    case ProtocolType.ModbusTCP:
                        // 提取参数
                        var modbusIp = channel.Configuration.ContainsKey("ipAddress") ? channel.Configuration["ipAddress"] : null;
                        var modbusPortStr = channel.Configuration.ContainsKey("port") ? channel.Configuration["port"] : "502";
                        if (string.IsNullOrWhiteSpace(modbusIp))
                            throw new ArgumentException("ModbusTCP配置缺少ipAddress参数");
                        if (!int.TryParse(modbusPortStr, out int modbusPort))
                            modbusPort = 502;
                        // 实例化ModbusTcpClient
                        return new ModbusTcpClient(modbusIp, modbusPort);

                    default:
                        // 其他协议暂未实现
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"CreatClientByChannel参数解析或实例化失败，协议: {channel.Protocol}");
                return null;
            }
        }

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

        public  DataTypeEnums ToDataTypeEnums(DataType dataType)
        {
            // 使用 Enum.TryParse 保证安全转换
            if (Enum.TryParse<DataTypeEnums>(dataType.ToString(), out var result))
            {
                return result;
            }
            return DataTypeEnums.None;
        }

        public  DataType ToDataType(DataTypeEnums dataTypeEnum)
        {
            if (Enum.TryParse<DataType>(dataTypeEnum.ToString(), out var result))
            {
                return result;
            }
            return DataType.None;
        }
    }
} 