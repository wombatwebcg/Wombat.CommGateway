using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.DataTypeExtensions;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;
using Wombat.CommGateway.Domain.Repositories;


namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 网关设备服务实现
    /// </summary>
    [AutoInject(typeof(IGatewayDeviceService), ServiceLifetime = ServiceLifetime.Scoped)]
    public class GatewayDeviceService : IGatewayDeviceService
    {
        private readonly IGatewayDeviceRepository _deviceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GatewayDeviceService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="deviceRepository">设备仓储</param>
        /// <param name="mapper">AutoMapper实例</param>
        /// <param name="logger">日志记录器</param>
        public GatewayDeviceService(
            IGatewayDeviceRepository deviceRepository,
            IMapper mapper,
            ILogger<GatewayDeviceService> logger)
        {
            _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<List<GatewayDeviceDto>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("开始获取所有设备列表");

                var devices = await _deviceRepository.GetAllAsync();
                var dtos = _mapper.Map<List<GatewayDeviceDto>>(devices);

                _logger.LogInformation("获取所有设备列表成功，数量: {Count}", dtos.Count);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有设备列表失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<GatewayDeviceDto> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("开始获取设备详情: {Id}", id);

                var device = await _deviceRepository.GetByIdAsync(id);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{id}的设备");

                var dto = _mapper.Map<GatewayDeviceDto>(device);

                _logger.LogInformation("获取设备详情成功: {Id}", id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备详情失败: {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> CreateAsync(CreateGatewayDeviceDto dto)
        {
            try
            {
                _logger.LogInformation("开始创建设备: {Name}", dto.Name);

                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));
                if (string.IsNullOrWhiteSpace(dto.Name))
                    throw new ArgumentException("设备名称不能为空", nameof(dto.Name));
                if (string.IsNullOrWhiteSpace(dto.Address))
                    throw new ArgumentException("设备地址不能为空", nameof(dto.Address));

                var device = new GatewayDevice(dto.Name, dto.Type, dto.Address);
                if (!string.IsNullOrWhiteSpace(dto.Description))
                    device.UpdateProperties(new Dictionary<string, string> { { "Description", dto.Description } });
                if (dto.ChannelId > 0)
                    device.UpdateChannelId(dto.ChannelId);
                if (dto.ProtocolConfigId > 0)
                    device.UpdateProtocolConfigId(dto.ProtocolConfigId);
                if (dto.Properties != null)
                    device.UpdateProperties(dto.Properties);

                await _deviceRepository.InsertAsync(device);

                _logger.LogInformation("设备创建成功: {Id}", device.Id);
                return device.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建设备失败: {Name}", dto?.Name);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(int id, UpdateGatewayDeviceDto dto)
        {
            try
            {
                _logger.LogInformation("开始更新设备: {Id}", id);

                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));

                var device = await _deviceRepository.GetByIdAsync(id);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{id}的设备");

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    device.UpdateName(dto.Name);
                if (!string.IsNullOrWhiteSpace(dto.Description))
                    device.UpdateProperties(new Dictionary<string, string> { { "Description", dto.Description } });
                if (dto.Type != default)
                    device.UpdateDeviceType(dto.Type);
                if (!string.IsNullOrWhiteSpace(dto.Address))
                    device.UpdateAddress(dto.Address);
                if (dto.ChannelId > 0)
                    device.UpdateChannelId(dto.ChannelId);
                if (dto.ProtocolConfigId > 0)
                    device.UpdateProtocolConfigId(dto.ProtocolConfigId);
                if (dto.Properties != null)
                    device.UpdateProperties(dto.Properties);

                await _deviceRepository.UpdateAsync(device);

                _logger.LogInformation("设备更新成功: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新设备失败: {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("开始删除设备: {Id}", id);

                var device = await _deviceRepository.GetByIdAsync(id);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{id}的设备");

                await _deviceRepository.DeleteAsync(id);

                _logger.LogInformation("设备删除成功: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除设备失败: {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdateDeviceStatusAsync(int deviceId, DeviceStatus status)
        {
            try
            {
                _logger.LogInformation("开始更新设备状态: {DeviceId}, {Status}", deviceId, status);

                var device = await _deviceRepository.GetByIdAsync(deviceId);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{deviceId}的设备");

                device.UpdateStatus(status);
                await _deviceRepository.UpdateAsync(device);

                _logger.LogInformation("设备状态更新成功: {DeviceId}, {Status}", deviceId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新设备状态失败: {DeviceId}, {Status}", deviceId, status);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(int id)
        {
            await UpdateDeviceStatusAsync(id, DeviceStatus.Running);
        }

        /// <inheritdoc/>
        public async Task StopAsync(int id)
        {
            await UpdateDeviceStatusAsync(id, DeviceStatus.Stopped);
        }

        /// <inheritdoc/>
        public async Task AddPointAsync(int deviceId, CreateDevicePointDto dto)
        {
            try
            {
                _logger.LogInformation("开始添加设备点位: {DeviceId}", deviceId);

                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));
                if (string.IsNullOrWhiteSpace(dto.Name))
                    throw new ArgumentException("点位名称不能为空", nameof(dto.Name));
                if (string.IsNullOrWhiteSpace(dto.Address))
                    throw new ArgumentException("点位地址不能为空", nameof(dto.Address));
                if (string.IsNullOrWhiteSpace(dto.DataType))
                    throw new ArgumentException("数据类型不能为空", nameof(dto.DataType));
                if (dto.ScanRate <= 0)
                    throw new ArgumentException("扫描周期必须大于0", nameof(dto.ScanRate));

                var device = await _deviceRepository.GetByIdAsync(deviceId);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{deviceId}的设备");

                var point = new DevicePoint(dto.Name, dto.Address, Enum.Parse<DataTypeEnums>(dto.DataType), dto.ScanRate);
                if (dto.Properties != null)
                    point.UpdateProperties(dto.Properties);

                device.AddPoint(point);
                await _deviceRepository.UpdateAsync(device);

                _logger.LogInformation("设备点位添加成功: {DeviceId}, {PointId}", deviceId, point.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加设备点位失败: {DeviceId}", deviceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdatePointAsync(int deviceId, int pointId, UpdateDevicePointDto dto)
        {
            try
            {
                _logger.LogInformation("开始更新设备点位: {DeviceId}, {PointId}", deviceId, pointId);

                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));

                var device = await _deviceRepository.GetByIdAsync(deviceId);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{deviceId}的设备");

                var point = device.Points.Find(p => p.Id == pointId);
                if (point == null)
                    throw new KeyNotFoundException($"未找到ID为{pointId}的点位");

                if (!string.IsNullOrWhiteSpace(dto.Name))
                    point.UpdateName(dto.Name);
                if (!string.IsNullOrWhiteSpace(dto.Address))
                    point.UpdateAddress(dto.Address);
                if (!string.IsNullOrWhiteSpace(dto.DataType))
                    point.UpdateDataType(Enum.Parse<DataTypeEnums>(dto.DataType));
                if (dto.ScanRate > 0)
                    point.UpdateScanRate(dto.ScanRate);
                if (dto.IsEnabled.HasValue)
                    point.UpdateStatus(dto.IsEnabled.Value);
                if (dto.Properties != null)
                    point.UpdateProperties(dto.Properties);

                await _deviceRepository.UpdateAsync(device);

                _logger.LogInformation("设备点位更新成功: {DeviceId}, {PointId}", deviceId, pointId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新设备点位失败: {DeviceId}, {PointId}", deviceId, pointId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeletePointAsync(int deviceId, int pointId)
        {
            try
            {
                _logger.LogInformation("开始删除设备点位: {DeviceId}, {PointId}", deviceId, pointId);

                var device = await _deviceRepository.GetByIdAsync(deviceId);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{deviceId}的设备");

                var point = device.Points.Find(p => p.Id == pointId);
                if (point == null)
                    throw new KeyNotFoundException($"未找到ID为{pointId}的点位");

                device.RemovePoint(pointId);
                await _deviceRepository.UpdateAsync(device);

                _logger.LogInformation("设备点位删除成功: {DeviceId}, {PointId}", deviceId, pointId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除设备点位失败: {DeviceId}, {PointId}", deviceId, pointId);
                throw;
            }
        }

        public async Task<GatewayDevice> CreateDeviceAsync(string name, string description, DeviceType type)
        {
            try
            {
                _logger.LogInformation("开始创建设备: {Name}", name);

                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("设备名称不能为空", nameof(name));

                var device = new GatewayDevice(name, type, string.Empty);
                if (!string.IsNullOrWhiteSpace(description))
                    device.UpdateProperties(new Dictionary<string, string> { { "Description", description } });

                await _deviceRepository.InsertAsync(device);

                _logger.LogInformation("设备创建成功: {Id}", device.Id);
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建设备失败: {Name}", name);
                throw;
            }
        }

        public async Task<IEnumerable<GatewayDevice>> GetDevicesAsync()
        {
            try
            {
                _logger.LogInformation("开始获取所有设备列表");

                var devices = await _deviceRepository.GetAllAsync();

                _logger.LogInformation("获取所有设备列表成功，数量: {Count}", devices.Count);
                return devices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有设备列表失败");
                throw;
            }
        }

        public async Task<GatewayDevice> GetDeviceByIdAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation("开始获取设备详情: {DeviceId}", deviceId);

                var device = await _deviceRepository.GetByIdAsync(deviceId);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{deviceId}的设备");

                _logger.LogInformation("获取设备详情成功: {DeviceId}", deviceId);
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备详情失败: {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task UpdateDevicePropertiesAsync(int deviceId, Dictionary<string, string> properties)
        {
            try
            {
                _logger.LogInformation("开始更新设备属性: {DeviceId}", deviceId);

                var device = await _deviceRepository.GetByIdAsync(deviceId);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{deviceId}的设备");

                device.UpdateProperties(properties);
                await _deviceRepository.UpdateAsync(device);

                _logger.LogInformation("设备属性更新成功: {DeviceId}", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新设备属性失败: {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task DeleteDeviceAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation("开始删除设备: {DeviceId}", deviceId);

                var device = await _deviceRepository.GetByIdAsync(deviceId);
                if (device == null)
                    throw new KeyNotFoundException($"未找到ID为{deviceId}的设备");

                await _deviceRepository.DeleteAsync(deviceId);

                _logger.LogInformation("设备删除成功: {DeviceId}", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除设备失败: {DeviceId}", deviceId);
                throw;
            }
        }
    }
} 