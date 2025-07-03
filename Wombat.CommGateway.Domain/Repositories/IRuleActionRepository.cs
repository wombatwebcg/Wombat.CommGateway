using System.Collections.Generic;
using System.Threading.Tasks;    
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 规则动作仓储接口
    /// </summary>
    public interface IRuleActionRepository : IRepositoryKey<RuleAction>
    {
        /// <summary>
        /// 获取所有规则动作
        /// </summary>
        Task<List<RuleAction>> GetAllAsync();

        /// <summary>
        /// 根据ID获取规则动作
        /// </summary>
        Task<RuleAction> GetByIdAsync(int id);

        /// <summary>
        /// 根据规则ID获取规则动作
        /// </summary>
        Task<List<RuleAction>> GetByRuleIdAsync(int id);



        /// <summary>
        /// 删除规则动作
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
} 