using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;
using Wombat.CommGateway.Domain.Enums;
using AutoMapper;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 设备服务实现
    /// </summary>
    [AutoInject(typeof(IDeviceService))]
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IMapper _mapper;

        public DeviceService(IDeviceRepository deviceRepository,IMapper mapper)
        {
            _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
            _mapper = mapper ?? throw new ArgumentNullException();
        }

        public async Task<DeviceResponseDto> GetDevicesAsync(DeviceQueryDto query)
        {
            var devices = await _deviceRepository.GetAllAsync();
            
            // 应用查询过滤
            var filteredDevices = devices.AsQueryable();
            
            if (!string.IsNullOrEmpty(query.Name))
            {
                filteredDevices = filteredDevices.Where(d => d.Name.Contains(query.Name));
            }
            
            


            var total = filteredDevices.Count();
            
            // 应用分页
            if (query.Page.HasValue && query.PageSize.HasValue)
            {
                filteredDevices = filteredDevices
                    .Skip((query.Page.Value - 1) * query.PageSize.Value)
                    .Take(query.PageSize.Value);
            }

            var deviceDtos = filteredDevices.Select(MapToDto).ToList();

            return new DeviceResponseDto
            {
                Items = deviceDtos,
                Total = total
            };
        }

        public async Task<List<DeviceDto>> GetAllDevicesAsync()
        {
            var devices = await _deviceRepository.GetAllAsync();
            if (devices != null)
            {
                var dtos = _mapper.Map<List<DeviceDto>>(devices);
                return dtos;
            }
            return null;
        }

        public async Task<DeviceDto> GetDeviceByIdAsync(int id)
        {
            var device = await _deviceRepository.GetByIdAsync(id);
            if (device != null)
            {
                var dto = _mapper.Map<DeviceDto>(device);
                return dto;

            }
            return null;

        }

        public async Task<int> CreateDeviceAsync(CreateDeviceDto dto)
        {
            var device = new Device(dto.Name, dto.DeviceGroupId,dto.ChannelId);
            device = _mapper.Map<Device>(dto);
            device.Enable = dto.Enable;
            await _deviceRepository.InsertAsync(device);
            return device.Id;
        }

        public async Task UpdateDeviceAsync(int id, UpdateDeviceDto dto)
        {
            var device = await _deviceRepository.GetByIdAsync(id);
            _mapper.Map(dto, device);
            device.Id = id;
            await _deviceRepository.UpdateAsync(device);
        }

        public async Task DeleteDeviceAsync(int id)
        {
            var device = await _deviceRepository.GetByIdAsync(id);
            if (device == null)
                throw new ArgumentException($"Device with id {id} not found.");

            await _deviceRepository.DeleteAsync(device);
        }

        public async Task StartDeviceAsync(int id)
        {
            var device = await _deviceRepository.GetByIdAsync(id);
            if (device == null)
                throw new ArgumentException($"Device with id {id} not found.");

            device.UpdateEnable(true);
            await _deviceRepository.UpdateAsync(device);
        }

        public async Task StopDeviceAsync(int id)
        {
            var device = await _deviceRepository.GetByIdAsync(id);
            if (device == null)
                throw new ArgumentException($"Device with id {id} not found.");

            device.UpdateEnable(false);
            await _deviceRepository.UpdateAsync(device);
        }

        public async Task UpdateDeviceStatusAsync(int id, bool enable)
        {
            var device = await _deviceRepository.GetByIdAsync(id);
            if (device == null)
                throw new ArgumentException($"Device with id {id} not found.");

            device.UpdateEnable(enable);
            await _deviceRepository.UpdateAsync(device);
        }

        public async Task UpdateDeviceEnableAsync(int id, bool enable)
        {
            var device = await _deviceRepository.GetByIdAsync(id);
            if (device == null)
                throw new ArgumentException($"Device with id {id} not found.");

            device.Enable = enable;
            await _deviceRepository.UpdateAsync(device);
        }

        private DeviceDto MapToDto(Device device)
        {
            return new DeviceDto
            {
                Id = device.Id,
                Name = device.Name,
                Description = device.Description,
                Enable = device.Enable,
                ChannelId = device.ChannelId,
                DeviceGroupId = device.DeviceGroupId, 
                Points = device.Points?.Select(p => new DevicePointDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Address = p.Address,
                    DataType = p.DataType,
                    ScanRate = p.ScanRate,
                    DeviceId = p.DeviceId
                }).ToList(),
                CreateTime = device.CreateTime,
                UpdateTime = device.UpdateTime
            };
        }


    }
} 