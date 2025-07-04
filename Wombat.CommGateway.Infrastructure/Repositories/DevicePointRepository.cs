using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Wombat.CommGateway.Domain.Repositories;
using NPOI.SS.Formula.Functions;

namespace Wombat.CommGateway.Infrastructure.Repositories
{
    /// <summary>
    /// 设备点位仓储实现
    /// </summary>

    [AutoInject(typeof(IDevicePointRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class DevicePointRepository : BaseRepository<DevicePoint, GatawayDB>, IDevicePointRepository
    {
        private readonly IServiceProvider _service;
        private readonly IDeviceRepository _deviceRepository;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="freeSql">FreeSql实例</param>
        public DevicePointRepository(IServiceProvider service, IDeviceRepository deviceRepository) : base(service)
        {
            _service = service;
            _deviceRepository = deviceRepository;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DevicePoint>> GetAllAsync()
        {
            var points = await Select.ToListAsync();
            return points;
        }

        /// <inheritdoc/>
        public async Task<DevicePoint> GetByIdAsync(int id)
        {
            return await Select.Where(x => x.Id == id).FirstAsync();
        }




        /// <inheritdoc/>
        public async Task<IEnumerable<DevicePoint>> GetDevicePointsAsync(int id)
        {
            return await Select.Where(x => x.DeviceId == id).ToListAsync();

        }

        /// <inheritdoc/>
        public async Task<DevicePoint> GetDevicePointAsync(int id)
        {
            return await Select.Where(x => x.Id == id).FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DevicePoint>> GetDevicePointByGrouopAsync(int id)
        {
            var deviveIds =await _deviceRepository.Select.Where(x => x.DeviceGroupId == id).ToListAsync();
            if (deviveIds != null)
            {
                List<DevicePoint> devicePoints = new List<DevicePoint>();
                foreach (var deviveId in deviveIds)
                {
                   var points = await Select.Where(x => x.DeviceId == deviveId.Id).ToListAsync();
                   if (points != null)
                   {
                    devicePoints.AddRange(points);
                   }
                }
                return devicePoints;
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDevicePointAsync(int id)
        {
            var count = await DeleteAsync(x => x.Id == id);
            return count > 0;
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
            var count = await UpdateAsync(devicePoints);
            return count > 0;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDevicePointsAsync(IEnumerable<int> ids)
        {
            var count = await DeleteAsync(x => ids.Contains(x.Id));
            return count > 0;
        }


    }
} 