using System;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 规则查询参数
    /// </summary>
    public class RuleQuery
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 规则名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 触发类型
        /// </summary>
        public string TriggerType { get; set; }

        /// <summary>
        /// 执行动作类型
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }
    }
} 