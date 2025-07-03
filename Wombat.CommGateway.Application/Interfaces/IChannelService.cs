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


        Task<int> UpdateStatusAsync(int id, int status);

        /// <summary>
        /// 更新通道配置
        /// </summary>
        Task<int> UpdateConfigurationAsync(int id, Dictionary<string, string> configuration);

        /// <summary>
        /// 更新通道启用状态
        /// </summary>
        Task<bool> UpdateEnableAsync(int id, bool enable);

        /// <summary>
        /// 更新通道
        /// </summary>
        Task<int> UpdateAsync(int id, UpdateChannelDto dto);

        /// <summary>
        /// 删除通道
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 启动通道
        /// </summary>
        Task<bool> StartAsync(int id);

        /// <summary>
        /// 停止通道
        /// </summary>
        Task<bool> StopAsync(int id);

        /// <summary>
        /// 获取实时数据
        /// </summary>
        Task<Dictionary<int, object>> GetRealtimeDataAsync(int deviceId);

    }
} 