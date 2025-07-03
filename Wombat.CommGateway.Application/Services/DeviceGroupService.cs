using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;
using Wombat.Infrastructure;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 设备组服务实现
    /// </summary>
    [AutoInject(typeof(IDeviceGroupService), ServiceLifetime = ServiceLifetime.Scoped)]
    public class DeviceGroupService : IDeviceGroupService
    {
        private readonly IDeviceGroupRepository _deviceGroupRepository;
        private readonly ILogger<DeviceGroupService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="deviceGroupRepository">设备组仓储</param>
        /// <param name="logger">日志记录器</param>
        public DeviceGroupService(
            IDeviceGroupRepository deviceGroupRepository,
            ILogger<DeviceGroupService> logger)
        {
            _deviceGroupRepository = deviceGroupRepository ?? throw new ArgumentNullException(nameof(deviceGroupRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<List<DeviceGroupDto>> GetAllDeviceGroupsAsync()
        {
            var deviceGroups = await _deviceGroupRepository.GetAllAsync();
            return deviceGroups.Select(MapToDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<DeviceGroupDto> GetDeviceGroupByIdAsync(int id)
        {
            var deviceGroup = await _deviceGroupRepository.GetByIdAsync(id);
            return deviceGroup == null ? null : MapToDto(deviceGroup);
        }

        /// <inheritdoc/>
        public async Task<DeviceGroupDto> CreateDeviceGroupAsync(CreateDeviceGroupDto dto)
        {
            var deviceGroup = new DeviceGroup(dto.Name, dto.Description);
            await _deviceGroupRepository.InsertAsync(deviceGroup);
            return MapToDto(deviceGroup);
        }

        /// <inheritdoc/>
        public async Task<DeviceGroupDto> UpdateDeviceGroupAsync(int id, UpdateDeviceGroupDto dto)
        {
            var deviceGroup = await _deviceGroupRepository.GetByIdAsync(id);
            if (deviceGroup == null)
                throw new ArgumentException($"DeviceGroup with id {id} not found.");

            deviceGroup.UpdateInfo(dto.Name, dto.Description);
            await _deviceGroupRepository.UpdateAsync(deviceGroup);
            return MapToDto(deviceGroup);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDeviceGroupAsync(int id)
        {
            var deviceGroup = await _deviceGroupRepository.GetByIdAsync(id);
            if (deviceGroup == null)
                return false;

            await _deviceGroupRepository.DeleteAsync(deviceGroup);
            return true;
        }

        private DeviceGroupDto MapToDto(DeviceGroup deviceGroup)
        {
            return new DeviceGroupDto
            {
                Id = deviceGroup.Id,
                Name = deviceGroup.Name,
                Description = deviceGroup.Description,
                CreateTime = deviceGroup.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdateTime = deviceGroup.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }
} 