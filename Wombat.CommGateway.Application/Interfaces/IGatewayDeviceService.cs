using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Interfaces
{ 
    /// <summary>
    /// 网关设备管理服务接口
    /// </summary>
    public interface IGatewayDeviceService
    {
        /// <summary>
        /// 获取所有设备
        /// </summary>
        Task<List<GatewayDeviceDto>> GetAllAsync();

        /// <summary>
        /// 根据ID获取设备
        /// </summary>
        Task<GatewayDeviceDto> GetByIdAsync(int id);

        /// <summary>
        /// 创建设备
        /// </summary>
        Task<int> CreateAsync(CreateGatewayDeviceDto dto);

        /// <summary>
        /// 更新设备
        /// </summary>
        Task UpdateAsync(int id, UpdateGatewayDeviceDto dto);

        /// <summary>
        /// 删除设备
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// 更新设备状态
        /// </summary>
        Task UpdateDeviceStatusAsync(int deviceId, DeviceStatus status);

        /// <summary>
        /// 启动设备
        /// </summary>
        Task StartAsync(int id);

        /// <summary>
        /// 停止设备
        /// </summary>
        Task StopAsync(int id);

        /// <summary>
        /// 添加点位
        /// </summary>
        Task AddPointAsync(int deviceId, CreateDevicePointDto dto);

        /// <summary>
        /// 更新点位
        /// </summary>
        Task UpdatePointAsync(int deviceId, int pointId, UpdateDevicePointDto dto);

        /// <summary>
        /// 删除点位
        /// </summary>
        Task DeletePointAsync(int deviceId, int pointId);
    }
} 