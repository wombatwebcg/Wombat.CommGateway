using System;
using System.Collections.Generic;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 规则结果数据传输对象
    /// </summary>
    public class RuleResultDto
    {
        /// <summary>
        /// 结果ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 规则ID
        /// </summary>
        public int RuleId { get; set; }

        /// <summary>
        /// 执行结果
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// 执行状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime ExecutionTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 规则执行结果DTO
    /// </summary>
    public class RuleExecutionResultDto
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
        public List<ActionExecutionResultDto> ActionResults { get; set; }
    }

    /// <summary>
    /// 动作执行结果DTO
    /// </summary>
    public class ActionExecutionResultDto
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