using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Common
{
    /// <summary>
    /// 规则引擎
    /// </summary>
    public class RuleEngine
    {
        private readonly List<Rule> _rules;

        public RuleEngine(List<Rule> rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// 执行规则
        /// </summary>
        public async Task<Domain.Entities.RuleResult> ExecuteAsync(DataCollectionRecord record)
        {
            var result = new Domain.Entities.RuleResult
            {
                RecordId = record.Id,
                IsTriggered = false,
                Timestamp = DateTime.Now,
                RuleResults = new List<RuleExecutionResult>(),
                TriggeredRules = new List<Rule>()
            };

            try
            {
                foreach (var rule in _rules)
                {
                    if (await EvaluateRuleAsync(rule, record))
                    {
                        result.TriggeredRules.Add(rule);
                        result.IsTriggered = true;
                        result.RuleResults.Add(new RuleExecutionResult
                        {
                            RuleId = rule.Id,
                            IsTriggered = true,
                            Timestamp = DateTime.Now
                        });
                        await ExecuteActionsAsync(rule, record);
                    }
                }
            }
            catch (Exception ex)
            {
                result.RuleResults.Add(new RuleExecutionResult
                {
                    RuleId = 0,
                    IsTriggered = false,
                    Error = ex.Message,
                    Timestamp = DateTime.Now
                });
            }

            return result;
        }

        private async Task<bool> EvaluateRuleAsync(Rule rule, DataCollectionRecord record)
        {
            if (!rule.IsEnabled)
            {
                return false;
            }

            try
            {
                switch (rule.Type)
                {
                    case "Threshold":
                        return await EvaluateThresholdRuleAsync(rule, record);
                    case "TimeBased":
                        return await EvaluateTimeBasedRuleAsync(rule, record);
                    case "Conditional":
                        return await EvaluateConditionalRuleAsync(rule, record);
                    default:
                        throw new NotSupportedException($"Rule type {rule.Type} is not supported");
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> EvaluateThresholdRuleAsync(Rule rule, DataCollectionRecord record)
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<ThresholdRuleConfig>(rule.RuleDefinition);
            if (record.PointId != config.PointId)
            {
                return false;
            }

            var value = Convert.ToDouble(record.Value);
            return value >= config.Threshold;
        }

        private async Task<bool> EvaluateTimeBasedRuleAsync(Rule rule, DataCollectionRecord record)
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<TimeBasedRuleConfig>(rule.RuleDefinition);
            var currentTime = DateTime.Now.TimeOfDay;
            return currentTime >= config.StartTime && currentTime <= config.EndTime;
        }

        private async Task<bool> EvaluateConditionalRuleAsync(Rule rule, DataCollectionRecord record)
        {
            var config = System.Text.Json.JsonSerializer.Deserialize<ConditionalRuleConfig>(rule.RuleDefinition);
            if (record.PointId != config.PointId1)
            {
                return false;
            }

            var value1 = Convert.ToDouble(record.Value);
            var value2 = Convert.ToDouble(config.PointId2);

            switch (config.Condition.ToLower())
            {
                case "equals":
                    return value1 == value2;
                case "greaterthan":
                    return value1 > value2;
                case "lessthan":
                    return value1 < value2;
                case "greaterthanorequals":
                    return value1 >= value2;
                case "lessthanorequals":
                    return value1 <= value2;
                default:
                    throw new NotSupportedException($"Condition {config.Condition} is not supported");
            }
        }

        private async Task ExecuteActionsAsync(Rule rule, DataCollectionRecord record)
        {
            if (rule.Actions == null || rule.Actions.Count == 0)
            {
                return;
            }

            foreach (var action in rule.Actions)
            {
                try
                {
                    switch (action.Type.ToLower())
                    {
                        case "mqtt":
                            await ExecuteMqttActionAsync(action, record);
                            break;
                        case "http":
                            await ExecuteHttpActionAsync(action, record);
                            break;
                        case "database":
                            await ExecuteDatabaseActionAsync(action, record);
                            break;
                        case "email":
                            await ExecuteEmailActionAsync(action, record);
                            break;
                        case "sms":
                            await ExecuteSmsActionAsync(action, record);
                            break;
                        default:
                            throw new NotSupportedException($"Action type {action.Type} is not supported");
                    }
                }
                catch (Exception)
                {
                    // 记录错误但继续执行其他动作
                }
            }
        }

        private async Task ExecuteMqttActionAsync(RuleAction action, DataCollectionRecord record)
        {
            // TODO: 实现 MQTT 动作执行逻辑
            await Task.CompletedTask;
        }

        private async Task ExecuteHttpActionAsync(RuleAction action, DataCollectionRecord record)
        {
            // TODO: 实现 HTTP 动作执行逻辑
            await Task.CompletedTask;
        }

        private async Task ExecuteDatabaseActionAsync(RuleAction action, DataCollectionRecord record)
        {
            // TODO: 实现数据库动作执行逻辑
            await Task.CompletedTask;
        }

        private async Task ExecuteEmailActionAsync(RuleAction action, DataCollectionRecord record)
        {
            // TODO: 实现邮件动作执行逻辑
            await Task.CompletedTask;
        }

        private async Task ExecuteSmsActionAsync(RuleAction action, DataCollectionRecord record)
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
} 