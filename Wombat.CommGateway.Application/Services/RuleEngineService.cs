using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.Common;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;


namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 规则引擎服务实现
    /// </summary>
    /// 
    [AutoInject(typeof(IRuleEngineService))]
    public class RuleEngineService : IRuleEngineService
    {
        private readonly ILogger<RuleEngineService> _logger;
        private readonly IRuleRepository _ruleRepository;
        private readonly IRuleActionRepository _actionRepository;
        private readonly IRuleResultRepository _resultRepository;
        private readonly IDataCollectionService _dataCollectionService;
        private RuleEngine _ruleEngine;

        public RuleEngineService(
            ILogger<RuleEngineService> logger,
            IRuleRepository ruleRepository,
            IRuleActionRepository actionRepository,
            IRuleResultRepository resultRepository,
            IDataCollectionService dataCollectionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
            _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
            _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
            _dataCollectionService = dataCollectionService ?? throw new ArgumentNullException(nameof(dataCollectionService));
        }

        private async Task InitializeRuleEngineAsync()
        {
            if (_ruleEngine == null)
            {
                var rules = await _ruleRepository.GetEnabledRulesAsync();
                _ruleEngine = new RuleEngine(rules);
            }
        }

        public async Task<Rule> CreateRuleAsync(string name, string description, RuleType type)
        {
            _logger.LogInformation("Creating new rule: {Name}", name);

            var rule = new Rule
            {
                Name = name,
                Description = description,
                Type = type.ToString(),
                IsEnabled = true,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            await _ruleRepository.InsertAsync(rule);
            await InitializeRuleEngineAsync();
            _logger.LogInformation("Rule created successfully with ID: {Id}", rule.Id);

            return rule;
        }

        public async Task UpdateRuleStatusAsync(int ruleId, bool isEnabled)
        {
            _logger.LogInformation("Updating rule status: {RuleId} to {Status}", ruleId, isEnabled);

            var rule = await _ruleRepository.Select.Where(r => r.Id == ruleId).FirstAsync();
            if (rule == null)
            {
                throw new KeyNotFoundException($"Rule not found with ID: {ruleId}");
            }

            rule.IsEnabled = isEnabled;
            rule.UpdateTime = DateTime.Now;
            await _ruleRepository.UpdateAsync(rule);
            await InitializeRuleEngineAsync();
            _logger.LogInformation("Rule status updated successfully");
        }

        public async Task<IEnumerable<Rule>> GetRulesAsync()
        {
            _logger.LogInformation("Getting all rules");
            return await _ruleRepository.Select.ToListAsync();
        }

        public async Task<Rule> GetRuleByIdAsync(int ruleId)
        {
            _logger.LogInformation("Getting rule by ID: {RuleId}", ruleId);
            return await _ruleRepository.Select.Where(r => r.Id == ruleId).FirstAsync();
        }

        public async Task UpdateRuleConfigurationAsync(int ruleId, string ruleDefinition)
        {
            _logger.LogInformation("Updating rule configuration: {RuleId}", ruleId);

            var rule = await _ruleRepository.Select.Where(r => r.Id == ruleId).FirstAsync();
            if (rule == null)
            {
                throw new KeyNotFoundException($"Rule not found with ID: {ruleId}");
            }

            rule.RuleDefinition = ruleDefinition;
            rule.UpdateTime = DateTime.Now;
            await _ruleRepository.UpdateAsync(rule);
            _logger.LogInformation("Rule configuration updated successfully");
        }

        public async Task DeleteRuleAsync(int ruleId)
        {
            _logger.LogInformation("Deleting rule: {RuleId}", ruleId);

            var rule = await _ruleRepository.Select.Where(r => r.Id == ruleId).FirstAsync();
            if (rule == null)
            {
                throw new KeyNotFoundException($"Rule not found with ID: {ruleId}");
            }

            await _ruleRepository.DeleteAsync(rule);
            _logger.LogInformation("Rule deleted successfully");
        }

        public async Task<RuleAction> AddRuleActionAsync(int ruleId, string actionType, string config)
        {
            _logger.LogInformation("Adding action to rule: {RuleId}", ruleId);

            var rule = await _ruleRepository.Select.Where(r => r.Id == ruleId).FirstAsync();
            if (rule == null)
            {
                throw new KeyNotFoundException($"Rule not found with ID: {ruleId}");
            }

            var action = new RuleAction
            {
                RuleId = ruleId,
                Type = actionType,
                Config = config,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now
            };

            await _actionRepository.InsertAsync(action);
            _logger.LogInformation("Action added successfully with ID: {Id}", action.Id);

            return action;
        }

        public async Task RemoveRuleActionAsync(int ruleId, int actionId)
        {
            _logger.LogInformation("Removing action {ActionId} from rule {RuleId}", actionId, ruleId);

            var action = await _actionRepository.Select
                .Where(a => a.Id == actionId && a.RuleId == ruleId)
                .FirstAsync();

            if (action == null)
            {
                throw new KeyNotFoundException($"Action not found with ID: {actionId}");
            }

            await _actionRepository.DeleteAsync(action);
            _logger.LogInformation("Action removed successfully");
        }

        public async Task<Domain.Entities.RuleResult> ExecuteRuleAsync(int ruleId, Dictionary<string, object> inputData)
        {
            _logger.LogInformation("Executing rule: {RuleId}", ruleId);

            await InitializeRuleEngineAsync();
            var rule = await _ruleRepository.Select.Where(r => r.Id == ruleId).FirstAsync();
            if (rule == null)
            {
                throw new KeyNotFoundException($"Rule not found with ID: {ruleId}");
            }

            if (!rule.IsEnabled)
            {
                throw new InvalidOperationException($"Rule {ruleId} is disabled");
            }

            var result = new Domain.Entities.RuleResult
            {
                RecordId = 0,
                IsTriggered = false,
                Timestamp = DateTime.Now,
                RuleResults = new List<RuleExecutionResult>(),
                TriggeredRules = new List<Rule>()
            };

            try
            {
                switch (rule.Type)
                {
                    case "Threshold":
                        await ExecuteThresholdRuleAsync(rule, inputData, result);
                        break;
                    case "TimeBased":
                        await ExecuteTimeBasedRuleAsync(rule, inputData, result);
                        break;
                    case "Conditional":
                        await ExecuteConditionalRuleAsync(rule, inputData, result);
                        break;
                    default:
                        throw new NotSupportedException($"Rule type {rule.Type} is not supported");
                }

                var actions = await _actionRepository.Select.Where(a => a.RuleId == ruleId).ToListAsync();
                foreach (var action in actions)
                {
                    await ExecuteActionAsync(action, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing rule {RuleId}", ruleId);
                result.RuleResults.Add(new RuleExecutionResult
                {
                    RuleId = ruleId,
                    IsTriggered = false,
                    Error = ex.Message,
                    Timestamp = DateTime.Now
                });
            }

            await _resultRepository.InsertAsync(result);
            _logger.LogInformation("Rule execution completed");

            return result;
        }

        public async Task<IEnumerable<Domain.Entities.RuleResult>> GetRuleExecutionHistoryAsync(int ruleId, DateTime startTime, DateTime endTime)
        {
            _logger.LogInformation("Getting execution history for rule {RuleId}", ruleId);
            return await _resultRepository.GetAllAsync();
        }

        private async Task ExecuteThresholdRuleAsync(Rule rule, Dictionary<string, object> inputData, Domain.Entities.RuleResult result)
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<ThresholdRuleConfig>(rule.RuleDefinition);
            var value = Convert.ToDouble(inputData[config.PointId.ToString()]);
            
            var ruleResult = new RuleExecutionResult
            {
                RuleId = rule.Id,
                IsTriggered = value >= config.Threshold,
                Timestamp = DateTime.Now
            };

            result.RuleResults.Add(ruleResult);
            if (ruleResult.IsTriggered)
            {
                result.IsTriggered = true;
                result.TriggeredRules.Add(rule);
            }
        }

        private async Task ExecuteTimeBasedRuleAsync(Rule rule, Dictionary<string, object> inputData, Domain.Entities.RuleResult result)
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<TimeBasedRuleConfig>(rule.RuleDefinition);
            var currentTime = DateTime.Now.TimeOfDay;
            var isInTimeRange = currentTime >= config.StartTime && currentTime <= config.EndTime;

            var ruleResult = new RuleExecutionResult
            {
                RuleId = rule.Id,
                IsTriggered = isInTimeRange,
                Timestamp = DateTime.Now
            };

            result.RuleResults.Add(ruleResult);
            if (ruleResult.IsTriggered)
            {
                result.IsTriggered = true;
                result.TriggeredRules.Add(rule);
            }
        }

        private async Task ExecuteConditionalRuleAsync(Rule rule, Dictionary<string, object> inputData, Domain.Entities.RuleResult result)
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<ConditionalRuleConfig>(rule.RuleDefinition);
            var value1 = Convert.ToDouble(inputData[config.PointId1.ToString()]);
            var value2 = Convert.ToDouble(inputData[config.PointId2.ToString()]);
            bool isTriggered = false;

            switch (config.Condition.ToLower())
            {
                case "equals":
                    isTriggered = value1 == value2;
                    break;
                case "greaterthan":
                    isTriggered = value1 > value2;
                    break;
                case "lessthan":
                    isTriggered = value1 < value2;
                    break;
                case "greaterthanorequals":
                    isTriggered = value1 >= value2;
                    break;
                case "lessthanorequals":
                    isTriggered = value1 <= value2;
                    break;
                default:
                    throw new NotSupportedException($"Condition {config.Condition} is not supported");
            }

            var ruleResult = new RuleExecutionResult
            {
                RuleId = rule.Id,
                IsTriggered = isTriggered,
                Timestamp = DateTime.Now
            };

            result.RuleResults.Add(ruleResult);
            if (ruleResult.IsTriggered)
            {
                result.IsTriggered = true;
                result.TriggeredRules.Add(rule);
            }
        }

        private async Task ExecuteActionAsync(RuleAction action, Domain.Entities.RuleResult result)
        {
            _logger.LogInformation("Executing action {ActionId} of type {ActionType}", action.Id, action.Type);
            
            var actionResult = new ActionExecutionResult
            {
                ActionId = action.Id,
                IsSuccess = true
            };

            try
            {
                switch (action.Type.ToLower())
                {
                    case "mqtt":
                        await ExecuteMqttActionAsync(action);
                        break;
                    case "http":
                        await ExecuteHttpActionAsync(action);
                        break;
                    case "database":
                        await ExecuteDatabaseActionAsync(action);
                        break;
                    case "email":
                        await ExecuteEmailActionAsync(action);
                        break;
                    case "sms":
                        await ExecuteSmsActionAsync(action);
                        break;
                    default:
                        throw new NotSupportedException($"Action type {action.Type} is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {ActionId}", action.Id);
                actionResult.IsSuccess = false;
            }

            result.RuleResults.Last().ActionResults.Add(actionResult);
        }

        private async Task ExecuteMqttActionAsync(RuleAction action)
        {
            // TODO: 实现 MQTT 动作执行逻辑
            await Task.CompletedTask;
        }

        private async Task ExecuteHttpActionAsync(RuleAction action)
        {
            // TODO: 实现 HTTP 动作执行逻辑
            await Task.CompletedTask;
        }

        private async Task ExecuteDatabaseActionAsync(RuleAction action)
        {
            // TODO: 实现数据库动作执行逻辑
            await Task.CompletedTask;
        }

        private async Task ExecuteEmailActionAsync(RuleAction action)
        {
            // TODO: 实现邮件动作执行逻辑
            await Task.CompletedTask;
        }

        private async Task ExecuteSmsActionAsync(RuleAction action)
        {
            // TODO: 实现短信动作执行逻辑
            await Task.CompletedTask;
        }
    }

    public class ThresholdRuleConfig
    {
        public int PointId { get; set; }
        public double Threshold { get; set; }
    }

    public class TimeBasedRuleConfig
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class ConditionalRuleConfig
    {
        public int PointId1 { get; set; }
        public int PointId2 { get; set; }
        public string Condition { get; set; }
    }

    public class WritePointActionConfig
    {
        public int PointId { get; set; }
        public object Value { get; set; }
    }

    public class NotificationActionConfig
    {
        public string Recipient { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }

    public class ScriptActionConfig
    {
        public string Script { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
} 