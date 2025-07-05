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
using Wombat.CommGateway.Domain.Enums;
using Wombat.CommGateway.Domain.Repositories;
using Wombat.Infrastructure;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 设备点位服务实现
    /// 支持动态点位管理和实时通知
    /// </summary>
    /// 
    [AutoInject(typeof(IDevicePointService), ServiceLifetime = ServiceLifetime.Scoped)]

    public class DevicePointService : IDevicePointService
    {
        private readonly IDevicePointRepository _pointRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DevicePointService> _logger;
        private readonly IPointChangeNotificationService _pointChangeNotificationService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pointRepository">设备点位仓储</param>
        /// <param name="mapper">AutoMapper实例</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="pointChangeNotificationService">点位变更通知服务</param>
        public DevicePointService(
            IDevicePointRepository pointRepository,
            IMapper mapper,
            ILogger<DevicePointService> logger,
            IPointChangeNotificationService pointChangeNotificationService)
        {
            _pointRepository = pointRepository ?? throw new ArgumentNullException(nameof(pointRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pointChangeNotificationService = pointChangeNotificationService ?? throw new ArgumentNullException(nameof(pointChangeNotificationService));
        }

        /// <inheritdoc/>
        public async Task<PointListResponseDto> GetPointsAsync(PointQueryDto query)
        {
            var points = await _pointRepository.GetAllAsync();
            var filtered = points.AsQueryable();
            
            if (query.DeviceId.HasValue)
                filtered = filtered.Where(p => p.DeviceId == query.DeviceId.Value);
            if (query.DataType.HasValue)
                filtered = filtered.Where(p => (int)p.DataType == query.DataType.Value);
            if (query.Status.HasValue)
                filtered = filtered.Where(p => (int)p.Status == query.Status.Value);

                
            var total = filtered.Count();
            filtered = filtered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
            var dtoList = filtered.Select(MapToDto).ToList();
            return new PointListResponseDto { Items = dtoList, Total = total };
        }

        /// <inheritdoc/>
        public async Task<List<DevicePointDto>> GetAllPointsAsync()
        {
            var points = await _pointRepository.GetAllAsync();
            return points.Select(MapToDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<DevicePointDto> GetPointByIdAsync(int id)
        {
            var point = await _pointRepository.GetByIdAsync(id);
            return point == null ? null : MapToDto(point);
        }

        /// <inheritdoc/>

        public async Task<List<DevicePointDto>> GetPointByGroupIdAsync(int id)
        {
            var points = await _pointRepository.GetDevicePointByGrouopAsync(id);
            return points.Select(MapToDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<int> CreatePointAsync(CreateDevicePointDto dto)
        {
            try
            {
                var point = _mapper.Map<DevicePoint>(dto);           
                await _pointRepository.InsertAsync(point);
                
                _logger.LogInformation($"点位 {point.Id} 创建成功: {point.Name}");

                // 通知数据采集服务点位已创建
                try
                {
                    await _pointChangeNotificationService.OnPointCreatedAsync(point);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"通知点位创建失败: PointId={point.Id}");
                    // 不抛出异常，避免影响主业务流程
                }

                return point.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建点位失败: {dto.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdatePointAsync(int id, DevicePointDto dto)
        {
            try
            {
                var point = await _pointRepository.GetByIdAsync(id);
                if (point == null) 
                    throw new ArgumentException($"Point with id {id} not found.");

                // 保存更新前的值用于通知
                var oldScanRate = point.ScanRate;
                var oldEnable = point.Enable;
                    
                point.Name = dto.Name;
                point.DeviceId = dto.DeviceId;
                point.Address = dto.Address;
                point.DataType = dto.DataType;
                point.ReadWrite = dto.ReadWrite;
                point.ScanRate = dto.ScanRate;
                point.Enable = dto.Enable;
                point.Status = dto.Status;
                point.Remark = dto.Remark;
                point.UpdateTime = DateTime.Now;
                
                await _pointRepository.UpdateAsync(point);
                
                _logger.LogInformation($"点位 {id} 更新成功: {point.Name}");

                // 通知数据采集服务点位已更新
                try
                {
                    await _pointChangeNotificationService.OnPointUpdatedAsync(point, oldScanRate, oldEnable);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"通知点位更新失败: PointId={id}");
                    // 不抛出异常，避免影响主业务流程
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新点位失败: PointId={id}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeletePointAsync(int id)
        {
            try
            {
                var point = await _pointRepository.GetByIdAsync(id);
                if (point == null) 
                    throw new ArgumentException($"Point with id {id} not found.");
                    
                await _pointRepository.DeleteAsync(point);
                
                _logger.LogInformation($"点位 {id} 删除成功: {point.Name}");

                // 通知数据采集服务点位已删除
                try
                {
                    await _pointChangeNotificationService.OnPointDeletedAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"通知点位删除失败: PointId={id}");
                    // 不抛出异常，避免影响主业务流程
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除点位失败: PointId={id}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task UpdatePointEnableAsync(int id, bool enable)
        {
            try
            {
                var point = await _pointRepository.GetByIdAsync(id);
                if (point == null) 
                    throw new ArgumentException($"Point with id {id} not found.");

                var oldEnable = point.Enable;
                point.Enable = enable;
                point.UpdateTime = DateTime.Now;
                
                await _pointRepository.UpdateAsync(point);
                
                _logger.LogInformation($"点位 {id} 启用状态更新成功: {oldEnable} -> {enable}");

                // 通知数据采集服务点位启用状态变更
                try
                {
                    await _pointChangeNotificationService.OnPointEnabledChangedAsync(id, enable);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"通知点位启用状态变更失败: PointId={id}");
                    // 不抛出异常，避免影响主业务流程
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新点位启用状态失败: PointId={id}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<DevicePointDto>> GetDevicePointsAsync(int deviceId)
        {
            var points = await _pointRepository.Select.Where(p => p.DeviceId == deviceId).ToListAsync() ;
            if (points == null)
                return null;
            return _mapper.Map<List<DevicePointDto>>(points) ;
        }

        /// <inheritdoc/>
        public async Task UpdatePointStatusAsync(int pointId, bool enable)
        {
            var point = await _pointRepository.GetByIdAsync(pointId);
            if (point == null) 
                throw new ArgumentException($"Point with id {pointId} not found.");
            point.Enable = enable;
            await _pointRepository.UpdateAsync(point);
        }

        /// <inheritdoc/>
        public async Task UpdatePointConfigurationAsync(int pointId, UpdateDevicePointDto updateDevicePointDto)
        {
            try
            {
                var point = await _pointRepository.GetByIdAsync(pointId);
                if (point == null) 
                    throw new ArgumentException($"Point with id {pointId} not found.");

                // 保存更新前的值用于通知
                var oldScanRate = point.ScanRate;
                var oldEnable = point.Enable;
                    
                if (!string.IsNullOrEmpty(updateDevicePointDto.Name))
                    point.UpdateName(updateDevicePointDto.Name);

                if (updateDevicePointDto.ScanRate > 0)
                    point.ScanRate = updateDevicePointDto.ScanRate;

                if (updateDevicePointDto.Enable.HasValue)
                    point.Enable = updateDevicePointDto.Enable.Value;
                    
                await _pointRepository.UpdateAsync(point);
                
                _logger.LogInformation($"点位配置 {pointId} 更新成功");

                // 通知数据采集服务点位配置已更新
                try
                {
                    await _pointChangeNotificationService.OnPointUpdatedAsync(point, oldScanRate, oldEnable);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"通知点位配置更新失败: PointId={pointId}");
                    // 不抛出异常，避免影响主业务流程
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新点位配置失败: PointId={pointId}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task ImportPointsAsync(int deviceId, IEnumerable<DevicePoint> points)
        {
            try
            {
                await _pointRepository.AddDevicePointsAsync(points);
                
                var pointArray = points.ToArray();
                _logger.LogInformation($"批量导入点位成功: 设备 {deviceId}, 共 {pointArray.Length} 个点位");

                // 通知数据采集服务批量点位已导入
                try
                {
                    await _pointChangeNotificationService.OnPointsBatchImportedAsync(pointArray);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"通知批量点位导入失败: DeviceId={deviceId}");
                    // 不抛出异常，避免影响主业务流程
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量导入点位失败: DeviceId={deviceId}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DevicePointDto>> ExportPointsAsync(int deviceId)
        {
            var points = await _pointRepository.GetAllAsync();
            var devicePoints = points.Where(p => p.DeviceId == deviceId);
            return devicePoints.Select(MapToDto);
        }

        private DevicePointDto MapToDto(DevicePoint point)
        {
            return new DevicePointDto
            {
                Id = point.Id,
                Name = point.Name,
                DeviceId = point.DeviceId,
                Address = point.Address,
                DataType = point.DataType,
                ReadWrite = point.ReadWrite,
                ScanRate = point.ScanRate,
                Enable = point.Enable,
                CreateTime = point.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = point.Status,
                Remark = point.Remark
            };
        }


    }
} 