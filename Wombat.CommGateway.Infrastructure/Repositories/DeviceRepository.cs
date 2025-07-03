using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;

namespace Wombat.CommGateway.Infrastructure.Repositories
{
    /// <summary>
    /// 网关设备仓储实现
    /// </summary>
    [AutoInject(typeof(IDeviceRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class DeviceRepository : BaseRepository<Device, GatawayDB>, IDeviceRepository
    {
        private readonly IServiceProvider _service;

        public DeviceRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<List<Device>> GetAllAsync()
        {
            return await Select
                .Include(d => d.Channel)
                .Include(d => d.DeviceGroup).ToListAsync();

        }

        /// <inheritdoc/>
        public async Task<Device> GetByIdAsync(int id)
        {
            return await Select
                .Include(d => d.Channel)
                .Include(d => d.DeviceGroup)
                .Where(d => d.Id == id)
                .FirstAsync();
        }





        /// <inheritdoc/>
        public async Task<Device> GetByNameAsync(string name)
        {
            return await Select
                .Include(d => d.Channel)
                .Include(d => d.DeviceGroup)
                .Where(d => d.Name == name)
                .FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<List<DevicePoint>> GetDevicePointsAsync(int deviceId)
        {
            var device = await GetByIdAsync(deviceId);
            return device?.Points ?? new List<DevicePoint>();
        }

        /// <inheritdoc/>
        public async Task AddDevicePointAsync(int deviceId, DevicePoint point)
        {
            var device = await GetByIdAsync(deviceId);
            if (device != null)
            {
                device.AddPoint(point);
                await UpdateAsync(device);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteDevicePointAsync(int deviceId, int pointId)
        {
            var device = await GetByIdAsync(deviceId);
            if (device != null)
            {
                device.RemovePoint(pointId);
                await UpdateAsync(device);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var device = await GetByIdAsync(id);
            if (device != null)
            {
                var count = await base.DeleteAsync(device);
                if (count > 0) return true;
            }
            return false;
        }
    }
} 