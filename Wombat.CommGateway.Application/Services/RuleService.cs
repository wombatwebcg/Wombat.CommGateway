using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.Common;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;


namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 规则服务实现
    /// </summary>
    [AutoInject(typeof(IRuleService), ServiceLifetime = ServiceLifetime.Scoped)]
    public class RuleService : IRuleService
    {
        private readonly IRuleRepository _ruleRepository;
        private readonly IRuleActionRepository _ruleActionRepository;
        private RuleEngine _ruleEngine;

        public RuleService(
            IRuleRepository ruleRepository,
            IRuleActionRepository ruleActionRepository)
        {
            _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
            _ruleActionRepository = ruleActionRepository ?? throw new ArgumentNullException(nameof(ruleActionRepository));
            _ruleEngine = new RuleEngine(new List<Rule>());
        }

        /// <inheritdoc/>
        public async Task<Wombat.CommGateway.Application.Common.PageResult<Rule>> GetPagedAsync(RuleQuery query)
        {
            var rules = await _ruleRepository.GetAllAsync();
            var filteredRules = rules
                .Where(r => string.IsNullOrEmpty(query.Name) || r.Name.Contains(query.Name))
                .Where(r => string.IsNullOrEmpty(query.TriggerType) || r.Type == query.TriggerType)
                .Where(r => string.IsNullOrEmpty(query.ActionType) || r.Actions.Any(a => a.Type.ToString() == query.ActionType))
                .Where(r => string.IsNullOrEmpty(query.Status) || r.Status == query.Status)
                .ToList();

            var total = filteredRules.Count;
            var items = filteredRules
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new Wombat.CommGateway.Application.Common.PageResult<Rule>
            {
                Total = total,
                Items = items
            };
        }

        /// <inheritdoc/>
        public async Task<Rule> GetByIdAsync(int id)
        {
            return await _ruleRepository.GetByIdAsync(id);
        }

        /// <inheritdoc/>
        public async Task<int> AddAsync(Rule rule)
        {
            await _ruleRepository.InsertAsync(rule);
            return rule.Id;
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateAsync(Rule rule)
        {
            await _ruleRepository.UpdateAsync(rule);
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
                return false;

            await _ruleRepository.DeleteAsync(rule);
            return true;
        }

        /// <inheritdoc/>
        public async Task<List<Rule>> GetEnabledRulesAsync()
        {
            return await _ruleRepository.GetEnabledRulesAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Rule>> GetByDeviceIdAsync(int deviceId)
        {
            return await _ruleRepository.Select
                .Where(r => r.DeviceId == deviceId)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Rule>> GetByPointIdAsync(int pointId)
        {
            return await _ruleRepository.Select
                .Where(r => r.PointId == pointId)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Domain.Entities.RuleResult> ExecuteAsync(DataCollectionRecord record)
        {
            var rules = await _ruleRepository.GetEnabledRulesAsync();
            _ruleEngine = new RuleEngine(rules);
            var result = await _ruleEngine.ExecuteAsync(record);
            return new Domain.Entities.RuleResult
            {
                RecordId = record.Id,
                IsTriggered = result.IsTriggered,
                Timestamp = result.Timestamp,
                RuleResults = result.TriggeredRules.Select(r => new RuleExecutionResult
                {
                    RuleId = r.Id,
                    IsTriggered = true,
                    Timestamp = DateTime.Now
                }).ToList(),
                TriggeredRules = result.TriggeredRules
            };
        }

        /// <inheritdoc/>
        public async Task<DTOs.RuleTestResult> TestAsync(int id, RuleTestData testData)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
            {
                throw new KeyNotFoundException($"Rule with ID {id} not found.");
            }

            var result = DTOs.RuleTestResult.CreateSuccess(
                testData,
                new Dictionary<string, object>(),
                0,
                rule.Id,
                rule.Name
            );

            try
            {
                // 创建测试数据记录
                var record = new DataCollectionRecord
                {
                    DeviceId = testData.DeviceId,
                    PointId = testData.PointId,
                    Value = testData.TriggerValue,
                    Timestamp = testData.Timestamp,
                    Quality = "GOOD"
                };

                // 执行规则
                var ruleResult = await ExecuteAsync(record);
                result.OutputData = new Dictionary<string, object>
                {
                    { "IsTriggered", ruleResult.IsTriggered },
                    { "Timestamp", ruleResult.Timestamp },
                    { "RuleResults", ruleResult.RuleResults }
                };
            }
            catch (Exception ex)
            {
                result = DTOs.RuleTestResult.CreateFailure(
                    testData,
                    ex.Message,
                    rule.Id,
                    rule.Name
                );
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> EnableAsync(int id)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
            {
                throw new KeyNotFoundException($"Rule with ID {id} not found.");
            }

            rule.Status = "Enabled";
            await _ruleRepository.UpdateAsync(rule);
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> DisableAsync(int id)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
            {
                throw new KeyNotFoundException($"Rule with ID {id} not found.");
            }

            rule.Status = "Disabled";
            await _ruleRepository.UpdateAsync(rule);
            return true;
        }
    }
} 