using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;


namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 协议配置仓储接口
    /// </summary>
    public interface IProtocolConfigRepository : IRepositoryKey<ProtocolConfig>
    {
        /// <summary>
        /// 获取所有协议配置
        /// </summary>
        Task<List<ProtocolConfig>> GetAllAsync();

        /// <summary>
        /// 根据ID获取协议配置
        /// </summary>
        Task<ProtocolConfig> GetByIdAsync(int id);




        /// <summary>
        /// 删除协议配置
        /// </summary>
         Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 根据名称获取协议配置
        /// </summary>
        Task<ProtocolConfig> GetByNameAsync(string name);

        /// <summary>
        /// 获取所有启用的协议配置
        /// </summary>
        Task<List<ProtocolConfig>> GetEnabledConfigsAsync();
    }
} 