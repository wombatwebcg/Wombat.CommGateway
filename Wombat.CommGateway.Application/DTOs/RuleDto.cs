using System;
using System.Collections.Generic;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 规则数据传输对象
    /// </summary>
    public class RuleDto
    {
        /// <summary>
        /// 规则ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 规则名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 规则类型
        /// </summary>
        public string RuleType { get; set; }

        /// <summary>
        /// 规则配置
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 点位ID
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 规则条件
        /// </summary>
        public RuleConditionDto Condition { get; set; }

        /// <summary>
        /// 规则动作列表
        /// </summary>
        public List<RuleActionDto> Actions { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 规则条件DTO
    /// </summary>
    public class RuleConditionDto
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

    /// <summary>
    /// 规则动作DTO
    /// </summary>
    public class RuleActionDto
    {
        /// <summary>
        /// 动作ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 规则ID
        /// </summary>
        public int RuleId { get; set; }

        /// <summary>
        /// 动作类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 动作参数
        /// </summary>
        public string Parameters { get; set; }

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
    /// 创建规则数据传输对象
    /// </summary>
    public class CreateRuleDto
    {
        /// <summary>
        /// 规则名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 规则类型
        /// </summary>
        public string RuleType { get; set; }

        /// <summary>
        /// 规则配置
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 点位ID
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 规则条件
        /// </summary>
        public CreateRuleConditionDto Condition { get; set; }

        /// <summary>
        /// 规则动作列表
        /// </summary>
        public List<CreateRuleActionDto> Actions { get; set; }
    }

    /// <summary>
    /// 创建规则条件DTO
    /// </summary>
    public class CreateRuleConditionDto
    {
        /// <summary>
        /// 操作符
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 比较值
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// 创建规则动作DTO
    /// </summary>
    public class CreateRuleActionDto
    {
        /// <summary>
        /// 动作类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 动作参数
        /// </summary>
        public string Parameters { get; set; }
    }

    /// <summary>
    /// 更新规则数据传输对象
    /// </summary>
    public class UpdateRuleDto
    {
        /// <summary>
        /// 规则ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 规则名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 规则类型
        /// </summary>
        public string RuleType { get; set; }

        /// <summary>
        /// 规则配置
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 点位ID
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 规则条件
        /// </summary>
        public UpdateRuleConditionDto Condition { get; set; }

        /// <summary>
        /// 规则动作列表
        /// </summary>
        public List<UpdateRuleActionDto> Actions { get; set; }
    }

    /// <summary>
    /// 更新规则条件DTO
    /// </summary>
    public class UpdateRuleConditionDto
    {
        /// <summary>
        /// 操作符
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 比较值
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// 更新规则动作DTO
    /// </summary>
    public class UpdateRuleActionDto
    {
        /// <summary>
        /// 动作类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 动作参数
        /// </summary>
        public string Parameters { get; set; }
    }
} 