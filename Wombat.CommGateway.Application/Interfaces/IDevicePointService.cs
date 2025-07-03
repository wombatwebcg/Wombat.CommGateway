using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Application.DTOs;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 设备点位管理服务接口
    /// </summary>
    public interface IDevicePointService
    {
        /// <summary>
        /// 创建设备点位
        /// </summary>
        /// <param name="name">点位名称</param>
        /// <param name="address">点位地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="scanRate">扫描周期（毫秒）</param>
        /// <returns>创建的点位信息</returns>
        Task<DevicePointDto> CreatePointAsync(CreateDevicePointDto createDevicePointDto);

        /// <summary>
        /// 更新点位状态
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="isEnabled">是否启用</param>
        Task UpdatePointStatusAsync(int pointId, bool isEnabled);

        /// <summary>
        /// 获取设备的所有点位
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>点位列表</returns>
        Task<IEnumerable<DevicePointDto>> GetDevicePointsAsync(int deviceId);

        /// <summary>
        /// 获取点位详情
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>点位详情</returns>
        Task<DevicePointDto> GetPointByIdAsync(int pointId);

        /// <summary>
        /// 更新点位配置
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="address">点位地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="scanRate">扫描周期（毫秒）</param>
        Task UpdatePointConfigurationAsync(int pointId, UpdateDevicePointDto updateDevicePointDto);

        /// <summary>
        /// 删除点位
        /// </summary>
        /// <param name="pointId">点位ID</param>
        Task DeletePointAsync(int pointId);

        /// <summary>
        /// 批量导入点位
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="points">点位列表</param>
        Task ImportPointsAsync(int deviceId, IEnumerable<DevicePoint> points);

        /// <summary>
        /// 批量导出点位
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>点位列表</returns>
        Task<IEnumerable<DevicePointDto>> ExportPointsAsync(int deviceId);
    }
} 