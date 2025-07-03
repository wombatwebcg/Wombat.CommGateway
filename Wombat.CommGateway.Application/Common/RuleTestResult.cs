using System;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Common
{
    /// <summary>
    /// 规则测试结果
    /// </summary>
    public class RuleTestResult
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
        /// 规则执行结果
        /// </summary>
        public RuleResult Result { get; set; }
    }
} 