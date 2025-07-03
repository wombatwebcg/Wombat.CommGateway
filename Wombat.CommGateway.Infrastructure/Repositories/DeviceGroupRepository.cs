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
    /// 设备组仓储实现
    /// </summary>
    [AutoInject(typeof(IDeviceGroupRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class DeviceGroupRepository : BaseRepository<DeviceGroup, GatawayDB>, IDeviceGroupRepository
    {
        private readonly IServiceProvider _service;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="service">服务提供者</param>
        public DeviceGroupRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DeviceGroup>> GetAllAsync()
        {
            return await Select.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<DeviceGroup> GetByIdAsync(int id)
        {
            return await Select.Where(x => x.Id == id).FirstAsync();
        }






    }
} 