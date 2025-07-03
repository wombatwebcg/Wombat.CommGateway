using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 通信通道服务接口
    /// </summary>
    public interface IChannelService
    {
        /// <summary>
        /// 获取通道列表
        /// </summary>
        Task<List<ChannelDto>> GetListAsync();

        /// <summary>
        /// 根据ID获取通道
        /// </summary>
        Task<ChannelDto> GetByIdAsync(int id);

        /// <summary>
        /// 创建通道
        /// </summary>
        Task<ChannelDto> CreateAsync(CreateChannelDto dto);


        Task<ChannelDto> UpdateStatusAsync(int id, ChannelStatus status);


        /// <summary>
        /// 更新通道
        /// </summary>
        Task<ChannelDto> UpdateAsync(int id, UpdateChannelDto dto);

        /// <summary>
        /// 删除通道
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 启动通道
        /// </summary>
        Task<ChannelDto> StartAsync(int id);

        /// <summary>
        /// 停止通道
        /// </summary>
        Task<ChannelDto> StopAsync(int id);

        /// <summary>
        /// 获取实时数据
        /// </summary>
        Task<Dictionary<int, object>> GetRealtimeDataAsync(int deviceId);

        /// <summary>
        /// 写入数据
        /// </summary>
        Task WriteDataAsync(int pointId, object value);

        /// <summary>
        /// 批量写入数据
        /// </summary>
        Task BatchWriteDataAsync(Dictionary<int, object> pointValues);



    }
} 