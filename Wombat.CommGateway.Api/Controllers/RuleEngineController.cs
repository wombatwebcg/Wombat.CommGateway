using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Application.Services;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 规则引擎控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RuleEngineController : ApiControllerBase
    {
        private readonly IRuleEngineService _ruleService;

        public RuleEngineController(IRuleEngineService ruleService)
        {
            _ruleService = ruleService;
        }

        /// <summary>
        /// 创建规则
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Rule>> CreateRule([FromBody] CreateRuleRequest request)
        {
            var rule = await _ruleService.CreateRuleAsync(request.Name, request.Description, request.Type);
            return Success(rule);
        }

        /// <summary>
        /// 更新规则状态
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateRuleStatus(int id, [FromBody] UpdateRuleStatusRequest request)
        {
            await _ruleService.UpdateRuleStatusAsync(id, request.IsEnabled);
            return Success();
        }

        /// <summary>
        /// 获取规则列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rule>>> GetRules()
        {
            var rules = await _ruleService.GetRulesAsync();
            return Success(rules);
        }

        /// <summary>
        /// 获取规则详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Rule>> GetRule(int id)
        {
            var rule = await _ruleService.GetRuleByIdAsync(id);
            if (rule == null)
                return NotFound();
            return Success(rule);
        }

        /// <summary>
        /// 更新规则配置
        /// </summary>
        [HttpPut("{id}/configuration")]
        public async Task<IActionResult> UpdateRuleConfiguration(int id, [FromBody] string configuration)
        {
            await _ruleService.UpdateRuleConfigurationAsync(id, configuration);
            return Success();
        }

        /// <summary>
        /// 删除规则
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            await _ruleService.DeleteRuleAsync(id);
            return Success();
        }

        /// <summary>
        /// 添加规则动作
        /// </summary>
        [HttpPost("{id}/actions")]
        public async Task<ActionResult<RuleAction>> AddRuleAction(int id, [FromBody] AddRuleActionRequest request)
        {
            var action = await _ruleService.AddRuleActionAsync(id, request.ActionType, request.Configuration);
            return Success(action);
        }

        /// <summary>
        /// 移除规则动作
        /// </summary>
        [HttpDelete("{ruleId}/actions/{actionId}")]
        public async Task<IActionResult> RemoveRuleAction(int ruleId, int actionId)
        {
            await _ruleService.RemoveRuleActionAsync(ruleId, actionId);
            return Success();
        }

        /// <summary>
        /// 执行规则
        /// </summary>
        [HttpPost("{id}/execute")]
        public async Task<ActionResult<RuleResult>> ExecuteRule(int id, [FromBody] Dictionary<string, object> inputData)
        {
            var result = await _ruleService.ExecuteRuleAsync(id, inputData);
            return Success(result);
        }

        /// <summary>
        /// 获取规则执行历史
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<RuleResult>>> GetRuleExecutionHistory(int id, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            var history = await _ruleService.GetRuleExecutionHistoryAsync(id, startTime, endTime);
            return Success(history);
        }
    }

    public class CreateRuleRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public RuleType Type { get; set; }
    }

    public class UpdateRuleStatusRequest
    {
        public bool IsEnabled { get; set; }
    }

    public class AddRuleActionRequest
    {
        public string ActionType { get; set; }
        public string Configuration { get; set; }
    }
} 