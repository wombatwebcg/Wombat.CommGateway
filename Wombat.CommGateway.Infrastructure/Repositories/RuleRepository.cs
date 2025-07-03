using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;

namespace Wombat.CommGateway.Infrastructure.Repositories
{
    /// <summary>
    /// 规则仓储
    /// </summary>
    [AutoInject(typeof(IRuleRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class RuleRepository : BaseRepository<Rule, GatawayDB>, IRuleRepository
    {
        private readonly IServiceProvider _service;

        public RuleRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <summary>
        /// 获取所有规则
        /// </summary>
        /// <returns>规则列表</returns>
        public async Task<List<Rule>> GetAllAsync()
        {
            return await Select
                .Include(r => r.Condition)
                .Include(r => r.Actions)
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取规则
        /// </summary>
        /// <param name="id">规则ID</param>
        /// <returns>规则</returns>
        public async Task<Rule> GetByIdAsync(int id)
        {
            return await Select
                .Include(r => r.Condition)
                .Include(r => r.Actions)
                .Where(r => r.Id == id)
                .FirstAsync();
        }




        /// <summary>
        /// 获取启用的规则
        /// </summary>
        /// <returns>规则列表</returns>
        public async Task<List<Rule>> GetEnabledRulesAsync()
        {
            return await Select
                .Include(r => r.Condition)
                .Include(r => r.Actions)
                .Where(r => r.IsEnabled)
                .ToListAsync();
        }

        /// <summary>
        /// 根据设备ID获取规则
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>规则列表</returns>
        public async Task<List<Rule>> GetByDeviceIdAsync(int id)
        {
            return await Select
                .Include(r => r.Condition)
                .Include(r => r.Actions)
                .Where(r => r.DeviceId == id)
                .ToListAsync();
        }

        /// <summary>
        /// 根据点位ID获取规则
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>规则列表</returns>
        public async Task<List<Rule>> GetByPointIdAsync(int id)
        {
            return await Select
                .Include(r => r.Condition)
                .Include(r => r.Actions)
                .Where(r => r.PointId == id)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Rule> GetByNameAsync(string name)
        {
            return await Select
                .Include(r => r.Actions)
                .Where(r => r.Name == name)
                .FirstAsync();
        }
    }
} 