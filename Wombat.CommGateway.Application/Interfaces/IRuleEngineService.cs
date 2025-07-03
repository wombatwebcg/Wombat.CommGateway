using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 规则引擎服务接口
    /// </summary>
    public interface IRuleEngineService
    {
        /// <summary>
        /// 创建规则
        /// </summary>
        Task<Rule> CreateRuleAsync(string name, string description, RuleType type);

        /// <summary>
        /// 更新规则状态
        /// </summary>
        Task UpdateRuleStatusAsync(int ruleId, bool isEnabled);

        /// <summary>
        /// 获取规则列表
        /// </summary>
        Task<IEnumerable<Rule>> GetRulesAsync();

        /// <summary>
        /// 获取规则详情
        /// </summary>
        Task<Rule> GetRuleByIdAsync(int ruleId);

        /// <summary>
        /// 更新规则配置
        /// </summary>
        Task UpdateRuleConfigurationAsync(int ruleId, string configuration);

        /// <summary>
        /// 删除规则
        /// </summary>
        Task DeleteRuleAsync(int ruleId);

        /// <summary>
        /// 添加规则动作
        /// </summary>
        Task<RuleAction> AddRuleActionAsync(int ruleId, string actionType, string configuration);

        /// <summary>
        /// 移除规则动作
        /// </summary>
        Task RemoveRuleActionAsync(int ruleId, int actionId);

        /// <summary>
        /// 执行规则
        /// </summary>
        Task<RuleResult> ExecuteRuleAsync(int ruleId, Dictionary<string, object> inputData);

        /// <summary>
        /// 获取规则执行历史
        /// </summary>
        Task<IEnumerable<RuleResult>> GetRuleExecutionHistoryAsync(int ruleId, DateTime startTime, DateTime endTime);
    }
} 