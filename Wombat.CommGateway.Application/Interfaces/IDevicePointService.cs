using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Application.DTOs;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 设备点位服务接口
    /// </summary>
    public interface IDevicePointService
    {
        Task<PointListResponseDto> GetPointsAsync(PointQueryDto query);
        Task<List<DevicePointDto>> GetAllPointsAsync();
        Task<DevicePointDto> GetPointByIdAsync(int id);
        Task<List<DevicePointDto>> GetPointByGroupIdAsync(int id);
        Task<int> CreatePointAsync(CreateDevicePointDto dto);
        Task UpdatePointAsync(int id, DevicePointDto dto);
        Task DeletePointAsync(int id);
        Task UpdatePointEnableAsync(int id, bool enable);

        Task UpdatePointStatusAsync(int pointId, bool isEnabled);

        Task<List<DevicePointDto>> GetDevicePointsAsync(int deviceId);


        Task UpdatePointConfigurationAsync(int pointId, UpdateDevicePointDto updateDevicePointDto);

        Task ImportPointsAsync(int deviceId, IEnumerable<DevicePoint> points);
        Task<IEnumerable<DevicePointDto>> ExportPointsAsync(int deviceId);
    }
} 