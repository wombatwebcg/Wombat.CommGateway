using System;
using System.Collections.Generic;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 规则结果
    /// </summary>
    public class RuleResult
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public int RecordId { get; set; }

        /// <summary>
        /// 是否触发
        /// </summary>
        public bool IsTriggered { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }


        /// <summary>
        /// 规则执行结果
        /// </summary>
        public List<RuleExecutionResult> RuleResults { get; set; }

        /// <summary>
        /// 触发的规则
        /// </summary>
        public List<Rule> TriggeredRules { get; set; }
    }

    /// <summary>
    /// 规则执行结果
    /// </summary>
    public class RuleExecutionResult
    {
        /// <summary>
        /// 规则ID
        /// </summary>
        public int RuleId { get; set; }

        /// <summary>
        /// 是否触发
        /// </summary>
        public bool IsTriggered { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 动作执行结果
        /// </summary>
        public List<ActionExecutionResult> ActionResults { get; set; }
    }

    /// <summary>
    /// 动作执行结果
    /// </summary>
    public class ActionExecutionResult
    {
        /// <summary>
        /// 动作ID
        /// </summary>
        public int ActionId { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; set; }
    }
} 