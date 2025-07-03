using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Infrastructure.Repositories;
using Wombat.CommGateway.Domain.Repositories;

namespace Wombat.CommGateway.Infrastructure.Repositories
{
    /// <summary>
    /// 规则动作仓储实现
    /// </summary>
    /// 

    [AutoInject(typeof(IRuleActionRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class RuleActionRepository : BaseRepository<RuleAction, GatawayDB>, IRuleActionRepository
    {
        private readonly IServiceProvider _service;
        
        public RuleActionRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<List<RuleAction>> GetAllAsync()
        {
            return await Select
                .Include(a => a.Rule)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<RuleAction> GetByIdAsync(int id)
        {
            return await Select
                .Include(a => a.Rule)
                .Where(a => a.Id == id)
                .FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<List<RuleAction>> GetByRuleIdAsync(int ruleId)
        {
            return await Select
                .Include(a => a.Rule)
                .Where(a => a.RuleId == ruleId)
                .ToListAsync();
        }

        /// <inheritdoc/>

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