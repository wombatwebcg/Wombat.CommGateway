using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 规则仓储接口
    /// </summary>
    public interface IRuleRepository : IRepositoryKey<Rule>
    {
        /// <summary>
        /// 获取所有规则
        /// </summary>
        Task<List<Rule>> GetAllAsync();

        /// <summary>
        /// 根据ID获取规则
        /// </summary>
        Task<Rule> GetByIdAsync(int id);





        /// <summary>
        /// 根据名称获取规则
        /// </summary>
        Task<Rule> GetByNameAsync(string name);

        /// <summary>
        /// 获取所有启用的规则
        /// </summary>
        Task<List<Rule>> GetEnabledRulesAsync();
    }
} 