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
using AutoMapper;

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
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="deviceGroupRepository">设备组仓储</param>
        /// <param name="logger">日志记录器</param>
        public DeviceGroupService(
            IDeviceGroupRepository deviceGroupRepository,
            ILogger<DeviceGroupService> logger,
            IMapper mapper)
        {
            _deviceGroupRepository = deviceGroupRepository ?? throw new ArgumentNullException(nameof(deviceGroupRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task<List<DeviceGroupDto>> GetAllDeviceGroupsAsync()
        {
            var deviceGroups = await _deviceGroupRepository.GetAllAsync();
            if (deviceGroups != null)
            {
                var dtos = _mapper.Map<List<DeviceGroupDto>>(deviceGroups);
                return dtos;
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<DeviceGroupDto> GetDeviceGroupByIdAsync(int id)
        {
            var deviceGroup = await _deviceGroupRepository.GetByIdAsync(id);
            if (deviceGroup == null)
                return null;
            return _mapper.Map<DeviceGroupDto>(deviceGroup);
        }

        /// <inheritdoc/>
        public async Task<DeviceGroupDto> CreateDeviceGroupAsync(CreateDeviceGroupDto dto)
        {
            var deviceGroup = new DeviceGroup(dto.Name, dto.Description);
            await _deviceGroupRepository.InsertAsync(deviceGroup);
            return _mapper.Map<DeviceGroupDto>(deviceGroup);
        }

        /// <inheritdoc/>
        public async Task<int> UpdateDeviceGroupAsync(int id, UpdateDeviceGroupDto dto)
        {
            var deviceGroup = await _deviceGroupRepository.GetByIdAsync(id);
            if (deviceGroup == null)
                throw new ArgumentException($"DeviceGroup with id {id} not found.");

            deviceGroup.UpdateInfo(dto.Name, dto.Description);
            return await _deviceGroupRepository.UpdateAsync(deviceGroup);
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


    }
} 