using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 规则执行结果
    /// </summary>
    public class RulResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime ExecutionTime { get; set; }

        /// <summary>
        /// 触发的规则列表
        /// </summary>
        public List<Rule> TriggeredRules { get; set; } = new List<Rule>();

        /// <summary>
        /// 执行结果
        /// </summary>
        public object Result { get; set; }
    }
} 