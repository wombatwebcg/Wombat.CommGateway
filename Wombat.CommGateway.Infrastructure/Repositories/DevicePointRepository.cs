using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Wombat.CommGateway.Domain.Repositories;

namespace Wombat.CommGateway.Infrastructure.Repositories
{
    /// <summary>
    /// 设备点位仓储实现
    /// </summary>

    [AutoInject(typeof(IDevicePointRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class DevicePointRepository : BaseRepository<DevicePoint, GatawayDB>, IDevicePointRepository
    {
        private readonly IServiceProvider _service;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="freeSql">FreeSql实例</param>
        public DevicePointRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DevicePoint>> GetAllAsync()
        {
            return await Select.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<DevicePoint> GetByIdAsync(int id)
        {
            return await Select.Where(x => x.Id == id).FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> InsertAsync(DevicePoint devicePoint)
        {
            await InsertAsync(devicePoint);
            return devicePoint.Id > 0;
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateAsync(DevicePoint devicePoint)
        {
            await UpdateAsync(devicePoint);
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(DevicePoint devicePoint)
        {
            await DeleteAsync(devicePoint);
            return true;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DevicePoint>> GetDevicePointsAsync(int deviceId)
        {
            var device = await _service.GetService<IDeviceRepository>()
                .GetByIdAsync(deviceId);
            return device?.Points ?? new List<DevicePoint>();
        }

        /// <inheritdoc/>
        public async Task<DevicePoint> GetDevicePointAsync(int id)
        {
            return await Select.Where(x => x.Id == id).FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDevicePointAsync(int id)
        {
            await DeleteAsync(x => x.Id == id);
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> AddDevicePointsAsync(IEnumerable<DevicePoint> devicePoints)
        {
            await InsertAsync(devicePoints);
            return devicePoints.Any(x => x.Id > 0);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateDevicePointsAsync(IEnumerable<DevicePoint> devicePoints)
        {
            await UpdateAsync(devicePoints);
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDevicePointsAsync(IEnumerable<int> ids)
        {
            await DeleteAsync(x => ids.Contains(x.Id));
            return true;
        }
    }
} 