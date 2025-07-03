using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 规则结果仓储接口
    /// </summary>
    public interface IRuleResultRepository : IRepositoryKey<RuleResult>
    {
        Task<List<RuleResult>> GetAllAsync();

        Task<RuleResult> GetByIdAsync(int id);

    }
} 