using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Wombat.CommGateway.Domain.Common;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 规则实体
    /// </summary>
    /// 

    [Table("Rules")]
    public class Rule:Entity
    {

        /// <summary>
        /// 规则名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 规则描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 规则类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 点位ID
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 规则定义
        /// </summary>
        public string RuleDefinition { get; set; }

        /// <summary>
        /// 规则条件
        /// </summary>
        public RuleCondition Condition { get; set; }

        /// <summary>
        /// 规则动作列表
        /// </summary>
        public List<RuleAction> Actions { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// 规则类型枚举
    /// </summary>
    public enum RuleType
    {
        DataMapping,
        SinglePointTrigger,
        MultiPointTrigger,
        TimeTrigger
    }

    /// <summary>
    /// 规则条件实体
    /// </summary>
    public class RuleCondition
    {
        /// <summary>
        /// 条件ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 规则ID
        /// </summary>
        public int RuleId { get; set; }

        /// <summary>
        /// 操作符
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 比较值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
} 