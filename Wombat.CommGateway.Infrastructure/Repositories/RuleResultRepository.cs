using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Domain.Entities;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Repositories;

namespace Wombat.CommGateway.Infrastructure.Repositories
{
    /// <summary>
    /// 规则结果仓储实现
    /// </summary>
    [AutoInject(typeof(IRuleResultRepository), ServiceLifetime.Scoped)]
    public class RuleResultRepository : BaseRepository<RuleResult, GatawayDB>, IRuleResultRepository
    {
        public RuleResultRepository(IServiceProvider service) : base(service) { }

        public async Task<List<RuleResult>> GetAllAsync() => await Select.ToListAsync();

        public async Task<RuleResult> GetByIdAsync(int id) => await Select.Where(r => r.RecordId == id).FirstAsync();

        public async Task<bool> DeleteAsync(int id)
        {
            var device = await GetByIdAsync(id);
            if (device != null)
            {
                var count = await base.DeleteAsync(device);
                if (count > 0) return true;
            }
            return false;
        }

    }
} 