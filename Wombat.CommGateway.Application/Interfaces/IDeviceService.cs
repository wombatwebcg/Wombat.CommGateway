using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 设备服务接口
    /// </summary>
    public interface IDeviceService
    {
        /// <summary>
        /// 获取设备列表（分页）
        /// </summary>
        Task<DeviceResponseDto> GetDevicesAsync(DeviceQueryDto query);

        /// <summary>
        /// 获取所有设备（不分页）
        /// </summary>
        Task<List<DeviceDto>> GetAllDevicesAsync();

        /// <summary>
        /// 根据ID获取设备详情
        /// </summary>
        Task<DeviceDto> GetDeviceByIdAsync(int id);

        /// <summary>
        /// 创建设备
        /// </summary>
        Task<int> CreateDeviceAsync(CreateDeviceDto dto);

        /// <summary>
        /// 更新设备
        /// </summary>
        Task UpdateDeviceAsync(int id, UpdateDeviceDto dto);

        /// <summary>
        /// 删除设备
        /// </summary>
        Task DeleteDeviceAsync(int id);

        /// <summary>
        /// 启动设备
        /// </summary>
        Task StartDeviceAsync(int id);

        /// <summary>
        /// 停止设备
        /// </summary>
        Task StopDeviceAsync(int id);

        /// <summary>
        /// 更新设备状态
        /// </summary>
        Task UpdateDeviceStatusAsync(int id, bool enable);

        /// <summary>
        /// 更新设备启用状态
        /// </summary>
        Task UpdateDeviceEnableAsync(int id, bool enable);
    }
} 