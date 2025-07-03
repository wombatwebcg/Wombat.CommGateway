using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 设备组仓储接口
    /// </summary>
    public interface IDeviceGroupRepository : IRepositoryKey<DeviceGroup>
    {
        /// <summary>
        /// 获取所有设备组
        /// </summary>
        /// <returns>所有设备组列表</returns>
        Task<IEnumerable<DeviceGroup>> GetAllAsync();

        /// <summary>
        /// 根据ID获取设备组
        /// </summary>
        /// <param name="id">设备组ID</param>
        /// <returns>设备组</returns>
        Task<DeviceGroup> GetByIdAsync(int id);


    }
} 