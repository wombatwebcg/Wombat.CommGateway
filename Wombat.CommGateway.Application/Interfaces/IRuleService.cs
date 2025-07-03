using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Application.Common;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 规则服务接口
    /// </summary>
    public interface IRuleService
    {
        /// <summary>
        /// 分页获取规则列表
        /// </summary>
        Task<PageResult<Rule>> GetPagedAsync(RuleQuery query);

        /// <summary>
        /// 根据ID获取规则
        /// </summary>
        Task<Rule> GetByIdAsync(int id);

        /// <summary>
        /// 添加规则
        /// </summary>
        Task<int> AddAsync(Rule rule);

        /// <summary>
        /// 更新规则
        /// </summary>
        Task<bool> UpdateAsync(Rule rule);

        /// <summary>
        /// 删除规则
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 获取已启用的规则
        /// </summary>
        Task<List<Rule>> GetEnabledRulesAsync();

        /// <summary>
        /// 根据设备ID获取规则
        /// </summary>
        Task<List<Rule>> GetByDeviceIdAsync(int deviceId);

        /// <summary>
        /// 根据点位ID获取规则
        /// </summary>
        Task<List<Rule>> GetByPointIdAsync(int pointId);

        /// <summary>
        /// 执行规则
        /// </summary>
        Task<Wombat.CommGateway.Domain.Entities.RuleResult> ExecuteAsync(DataCollectionRecord record);

        /// <summary>
        /// 测试规则
        /// </summary>
        Task<Wombat.CommGateway.Application.DTOs.RuleTestResult> TestAsync(int id, RuleTestData testData);

        /// <summary>
        /// 启用规则
        /// </summary>
        Task<bool> EnableAsync(int id);

        /// <summary>
        /// 禁用规则
        /// </summary>
        Task<bool> DisableAsync(int id);
    }
} 