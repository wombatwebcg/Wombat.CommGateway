using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 设备点位仓储接口
    /// </summary>
    /// 
    public interface IDevicePointRepository : IRepositoryKey<DevicePoint>
    {
        /// <summary>
        /// 获取设备点位列表
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>设备点位列表</returns>
        Task<IEnumerable<DevicePoint>> GetDevicePointsAsync(int deviceId);

        /// <summary>
        /// 获取设备点位
        /// </summary>
        /// <param name="id">点位ID</param>
        /// <returns>设备点位</returns>
        Task<DevicePoint> GetDevicePointAsync(int id);



        /// <summary>
        /// 删除设备点位
        /// </summary>
        /// <param name="id">点位ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteDevicePointAsync(int id);

        /// <summary>
        /// 批量添加设备点位
        /// </summary>
        /// <param name="devicePoints">设备点位列表</param>
        /// <returns>添加结果</returns>
        Task<bool> AddDevicePointsAsync(IEnumerable<DevicePoint> devicePoints);

        /// <summary>
        /// 批量更新设备点位
        /// </summary>
        /// <param name="devicePoints">设备点位列表</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdateDevicePointsAsync(IEnumerable<DevicePoint> devicePoints);

        /// <summary>
        /// 批量删除设备点位
        /// </summary>
        /// <param name="ids">点位ID列表</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteDevicePointsAsync(IEnumerable<int> ids);
    }
} 