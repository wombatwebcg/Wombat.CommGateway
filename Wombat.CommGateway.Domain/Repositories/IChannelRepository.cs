using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 通信通道仓储接口
    /// </summary>
    public interface IChannelRepository : IRepositoryKey<Channel>
    {
        /// <summary>
        /// 获取所有通信通道
        /// </summary>
        Task<List<Channel>> GetAllAsync();

        /// <summary>
        /// 根据ID获取通信通道
        /// </summary>
        Task<Channel> GetByIdAsync(int id);


        /// <summary>
        /// 删除通信通道
        /// </summary>
         Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 根据名称获取通信通道
        /// </summary>
        Task<Channel> GetByNameAsync(string name);

        /// <summary>
        /// 根据协议配置ID获取通信通道
        /// </summary>
        Task<List<Channel>> GetByProtocolConfigIdAsync(int protocolConfigId);

        /// <summary>
        /// 获取所有运行中的通信通道
        /// </summary>
        Task<List<Channel>> GetRunningChannelsAsync();
    }
} 