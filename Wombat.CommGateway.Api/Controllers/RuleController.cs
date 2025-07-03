using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Application.Services;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 规则控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RuleController : ApiControllerBase
    {
        private readonly IRuleService _ruleService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ruleService">规则服务</param>
        public RuleController(IRuleService ruleService)
        {
            _ruleService = ruleService;
        }

        /// <summary>
        /// 分页获取规则列表
        /// </summary>
        /// <param name="query">查询参数</param>
        /// <returns>规则列表</returns>
        [HttpGet]
        public async Task<ActionResult<PageResult<Rule>>> GetPaged([FromQuery] RuleQuery query)
        {
            var rules = await _ruleService.GetPagedAsync(query);
            return Success(rules);
        }

        /// <summary>
        /// 根据ID获取规则
        /// </summary>
        /// <param name="id">规则ID</param>
        /// <returns>规则</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Rule>> GetById(int id)
        {
            var rule = await _ruleService.GetByIdAsync(id);
            if (rule == null)
                return NotFound();

            return Success(rule);
        }

        /// <summary>
        /// 添加规则
        /// </summary>
        /// <param name="rule">规则</param>
        /// <returns>规则ID</returns>
        [HttpPost]
        public async Task<ActionResult<int>> Add(Rule rule)
        {
            var id = await _ruleService.AddAsync(rule);
            return Success(id);
        }

        /// <summary>
        /// 更新规则
        /// </summary>
        /// <param name="id">规则ID</param>
        /// <param name="rule">规则</param>
        /// <returns>是否成功</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<bool>> Update(int id, Rule rule)
        {
            if (id != rule.Id)
                return BadRequest();

            var result = await _ruleService.UpdateAsync(rule);
            return Success(result);
        }

        /// <summary>
        /// 删除规则
        /// </summary>
        /// <param name="id">规则ID</param>
        /// <returns>是否成功</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> Delete(int id)
        {
            var result = await _ruleService.DeleteAsync(id);
            return Success(result);
        }

        /// <summary>
        /// 获取启用的规则
        /// </summary>
        /// <returns>规则列表</returns>
        [HttpGet("enabled")]
        public async Task<ActionResult<List<Rule>>> GetEnabled()
        {
            var rules = await _ruleService.GetEnabledRulesAsync();
            return Success(rules);
        }

        /// <summary>
        /// 根据设备ID获取规则
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>规则列表</returns>
        [HttpGet("device/{deviceId}")]
        public async Task<ActionResult<List<Rule>>> GetByDeviceId(int deviceId)
        {
            var rules = await _ruleService.GetByDeviceIdAsync(deviceId);
            return Success(rules);
        }

        /// <summary>
        /// 根据点位ID获取规则
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>规则列表</returns>
        [HttpGet("point/{pointId}")]
        public async Task<ActionResult<List<Rule>>> GetByPointId(int pointId)
        {
            var rules = await _ruleService.GetByPointIdAsync(pointId);
            return Success(rules);
        }

        /// <summary>
        /// 执行规则
        /// </summary>
        /// <param name="record">数据采集记录</param>
        /// <returns>规则执行结果</returns>
        [HttpPost("execute")]
        public async Task<ActionResult<RuleResult>> Execute(DataCollectionRecord record)
        {
            var result = await _ruleService.ExecuteAsync(record);
            return Success(result);
        }

        /// <summary>
        /// 测试规则
        /// </summary>
        /// <param name="id">规则ID</param>
        /// <param name="testData">测试数据</param>
        /// <returns>测试结果</returns>
        [HttpPost("{id}/test")]
        public async Task<ActionResult<RuleTestResult>> Test(int id, [FromBody] RuleTestData testData)
        {
            var result = await _ruleService.TestAsync(id, testData);
            return Success(result);
        }

        /// <summary>
        /// 启用规则
        /// </summary>
        /// <param name="id">规则ID</param>
        /// <returns>是否成功</returns>
        [HttpPost("{id}/enable")]
        public async Task<ActionResult<bool>> Enable(int id)
        {
            var result = await _ruleService.EnableAsync(id);
            return Success(result);
        }

        /// <summary>
        /// 禁用规则
        /// </summary>
        /// <param name="id">规则ID</param>
        /// <returns>是否成功</returns>
        [HttpPost("{id}/disable")]
        public async Task<ActionResult<bool>> Disable(int id)
        {
            var result = await _ruleService.DisableAsync(id);
            return Success(result);
        }
    }
} 