using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.DataTypeExtensions;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;
using Wombat.CommGateway.Infrastructure.Repositories;
using Wombat.Infrastructure;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 设备点位服务实现
    /// </summary>
    /// 
    [AutoInject(typeof(IDevicePointService), ServiceLifetime = ServiceLifetime.Scoped)]

    public class DevicePointService : IDevicePointService
    {
        private readonly IDevicePointRepository _devicePointRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DevicePointService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="devicePointRepository">设备点位仓储</param>
        /// <param name="mapper">AutoMapper实例</param>
        /// <param name="logger">日志记录器</param>
        public DevicePointService(
            IDevicePointRepository devicePointRepository,
            IMapper mapper,
            ILogger<DevicePointService> logger)
        {
            _devicePointRepository = devicePointRepository ?? throw new ArgumentNullException(nameof(devicePointRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<DevicePointDto> CreatePointAsync(CreateDevicePointDto dto)
        {
            try
            {
                _logger.LogInformation("开始创建设备点位: {Name}, {Address}, {DataType}, {ScanRate}", dto.Name, dto.Address, dto.DataType, dto.ScanRate);

                if (string.IsNullOrWhiteSpace(dto.Name))
                    throw new ArgumentException("点位名称不能为空", nameof(dto.Name));
                if (string.IsNullOrWhiteSpace(dto.Address))
                    throw new ArgumentException("点位地址不能为空", nameof(dto.Address));
                if (string.IsNullOrWhiteSpace(dto.DataType))
                    throw new ArgumentException("数据类型不能为空", nameof(dto.DataType));
                if (dto.ScanRate <= 0)
                    throw new ArgumentException("扫描周期必须大于0", nameof(dto.ScanRate));

                var point = new DevicePoint(dto.Name, dto.Address, Enum.Parse<DataTypeEnums>(dto.DataType), dto.ScanRate);
                await _devicePointRepository.InsertAsync(point);

                _logger.LogInformation("设备点位创建成功: {PointId}", point.Id);
                return _mapper.Map<DevicePointDto>(point);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建设备点位失败: {Name}, {Address}, {DataType}, {ScanRate}", dto.Name, dto.Address, dto.DataType, dto.ScanRate);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdatePointStatusAsync(int pointId, bool isEnabled)
        {
            try
            {
                _logger.LogInformation("开始更新设备点位状态: {PointId}, {IsEnabled}", pointId, isEnabled);

                var point = await _devicePointRepository.GetDevicePointAsync(pointId);
                if (point == null)
                    throw new KeyNotFoundException($"未找到ID为{pointId}的设备点位");

                point.UpdateStatus(isEnabled);
                await _devicePointRepository.UpdateAsync(point);

                _logger.LogInformation("设备点位状态更新成功: {PointId}, {IsEnabled}", pointId, isEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新设备点位状态失败: {PointId}, {IsEnabled}", pointId, isEnabled);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DevicePointDto>> GetDevicePointsAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation("开始获取设备点位列表: {DeviceId}", deviceId);

                var points = await _devicePointRepository.GetDevicePointsAsync(deviceId);
                var dtos = _mapper.Map<IEnumerable<DevicePointDto>>(points);

                _logger.LogInformation("获取设备点位列表成功: {DeviceId}, 数量: {Count}", deviceId, dtos.Count());
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备点位列表失败: {DeviceId}", deviceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<DevicePointDto> GetPointByIdAsync(int pointId)
        {
            try
            {
                _logger.LogInformation("开始获取设备点位详情: {PointId}", pointId);

                var point = await _devicePointRepository.GetDevicePointAsync(pointId);
                if (point == null)
                    throw new KeyNotFoundException($"未找到ID为{pointId}的设备点位");

                var dto = _mapper.Map<DevicePointDto>(point);

                _logger.LogInformation("获取设备点位详情成功: {PointId}", pointId);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备点位详情失败: {PointId}", pointId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdatePointConfigurationAsync(int pointId, UpdateDevicePointDto dto)
        {
            try
            {
                _logger.LogInformation("开始更新设备点位配置: {PointId}, {Address}, {DataType}, {ScanRate}", pointId, dto.Address, dto.DataType, dto.ScanRate);

                if (string.IsNullOrWhiteSpace(dto.Address))
                    throw new ArgumentException("点位地址不能为空", nameof(dto.Address));
                if (string.IsNullOrWhiteSpace(dto.DataType))
                    throw new ArgumentException("数据类型不能为空", nameof(dto.DataType));
                if (dto.ScanRate <= 0)
                    throw new ArgumentException("扫描周期必须大于0", nameof(dto.ScanRate));

                var point = await _devicePointRepository.GetDevicePointAsync(pointId);
                if (point == null)
                    throw new KeyNotFoundException($"未找到ID为{pointId}的设备点位");

                point.UpdateScanRate(dto.ScanRate);
                await _devicePointRepository.UpdateAsync(point);

                _logger.LogInformation("设备点位配置更新成功: {PointId}", pointId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新设备点位配置失败: {PointId}, {Address}, {DataType}, {ScanRate}", pointId, dto.Address, dto.DataType, dto.ScanRate);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeletePointAsync(int pointId)
        {
            try
            {
                _logger.LogInformation("开始删除设备点位: {PointId}", pointId);

                var point = await _devicePointRepository.GetDevicePointAsync(pointId);
                if (point == null)
                    throw new KeyNotFoundException($"未找到ID为{pointId}的设备点位");

                await _devicePointRepository.DeleteDevicePointAsync(pointId);

                _logger.LogInformation("设备点位删除成功: {PointId}", pointId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除设备点位失败: {PointId}", pointId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task ImportPointsAsync(int deviceId, IEnumerable<DevicePoint> points)
        {
            try
            {
                _logger.LogInformation("开始导入设备点位: {DeviceId}, 数量: {Count}", deviceId, points.Count());

                if (points == null || !points.Any())
                    throw new ArgumentException("点位列表不能为空", nameof(points));

                await _devicePointRepository.AddDevicePointsAsync(points);

                _logger.LogInformation("设备点位导入成功: {DeviceId}, 数量: {Count}", deviceId, points.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入设备点位失败: {DeviceId}", deviceId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DevicePointDto>> ExportPointsAsync(int deviceId)
        {
            try
            {
                _logger.LogInformation("开始导出设备点位: {DeviceId}", deviceId);

                var points = await _devicePointRepository.GetDevicePointsAsync(deviceId);
                var dtos = _mapper.Map<IEnumerable<DevicePointDto>>(points);

                _logger.LogInformation("设备点位导出成功: {DeviceId}, 数量: {Count}", deviceId, dtos.Count());
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出设备点位失败: {DeviceId}", deviceId);
                throw;
            }
        }
    }
} 