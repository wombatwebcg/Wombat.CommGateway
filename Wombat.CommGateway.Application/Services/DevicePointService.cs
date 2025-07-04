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
    /// </summary>
    /// 
    [AutoInject(typeof(IDevicePointService), ServiceLifetime = ServiceLifetime.Scoped)]

    public class DevicePointService : IDevicePointService
    {
        private readonly IDevicePointRepository _pointRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DevicePointService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pointRepository">设备点位仓储</param>
        /// <param name="mapper">AutoMapper实例</param>
        /// <param name="logger">日志记录器</param>
        public DevicePointService(
            IDevicePointRepository pointRepository,
            IMapper mapper,
            ILogger<DevicePointService> logger)
        {
            _pointRepository = pointRepository ?? throw new ArgumentNullException(nameof(pointRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var point = _mapper.Map<DevicePoint>(dto);           
            await _pointRepository.InsertAsync(point);
            return point.Id;
        }

        /// <inheritdoc/>
        public async Task UpdatePointAsync(int id, DevicePointDto dto)
        {
            var point = await _pointRepository.GetByIdAsync(id);
            if (point == null) 
                throw new ArgumentException($"Point with id {id} not found.");
                
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
        }

        /// <inheritdoc/>
        public async Task DeletePointAsync(int id)
        {
            var point = await _pointRepository.GetByIdAsync(id);
            if (point == null) 
                throw new ArgumentException($"Point with id {id} not found.");
                
            await _pointRepository.DeleteAsync(point);
        }

        /// <inheritdoc/>
        public async Task UpdatePointEnableAsync(int id, bool enable)
        {
            var point = await _pointRepository.GetByIdAsync(id);
            if (point == null) 
                throw new ArgumentException($"Point with id {id} not found.");
                
            point.Enable = enable;
            point.Enable = enable;
            point.UpdateTime = DateTime.Now;
            
            await _pointRepository.UpdateAsync(point);
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
            var point = await _pointRepository.GetByIdAsync(pointId);
            if (point == null) 
                throw new ArgumentException($"Point with id {pointId} not found.");
                
            if (!string.IsNullOrEmpty(updateDevicePointDto.Name))
                point.UpdateName(updateDevicePointDto.Name);
                
            await _pointRepository.UpdateAsync(point);
        }

        /// <inheritdoc/>
        public async Task ImportPointsAsync(int deviceId, IEnumerable<DevicePoint> points)
        {
            await _pointRepository.AddDevicePointsAsync(points);
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