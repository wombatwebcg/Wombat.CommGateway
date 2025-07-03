using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.DTOs;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 设备组服务接口
    /// </summary>
    public interface IDeviceGroupService
    {
        /// <summary>
        /// 获取所有设备组
        /// </summary>
        /// <returns>设备组列表</returns>
        Task<List<DeviceGroupDto>> GetAllDeviceGroupsAsync();

        /// <summary>
        /// 根据ID获取设备组
        /// </summary>
        /// <param name="id">设备组ID</param>
        /// <returns>设备组信息</returns>
        Task<DeviceGroupDto> GetDeviceGroupByIdAsync(int id);

        /// <summary>
        /// 创建设备组
        /// </summary>
        /// <param name="dto">创建设备组请求</param>
        /// <returns>创建的设备组信息</returns>
        Task<DeviceGroupDto> CreateDeviceGroupAsync(CreateDeviceGroupDto dto);

        /// <summary>
        /// 更新设备组
        /// </summary>
        /// <param name="id">设备组ID</param>
        /// <param name="dto">更新设备组请求</param>
        /// <returns>更新后的设备组信息</returns>
        Task<DeviceGroupDto> UpdateDeviceGroupAsync(int id, UpdateDeviceGroupDto dto);

        /// <summary>
        /// 删除设备组
        /// </summary>
        /// <param name="id">设备组ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteDeviceGroupAsync(int id);
    }
} 