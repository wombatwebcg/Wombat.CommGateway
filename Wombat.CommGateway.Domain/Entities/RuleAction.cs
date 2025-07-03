using System;
using System.ComponentModel.DataAnnotations.Schema;
using Wombat.CommGateway.Domain.Common;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 规则动作实体
    /// </summary>
    /// 
    [Table("RuleActions")]

    public class RuleAction : Entity
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
        /// 动作名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 动作类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 动作配置(JSON格式)
        /// </summary>
        public string Config { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 关联的规则
        /// </summary>
        public virtual Rule Rule { get; set; }

        public RuleAction() { }

        public RuleAction(string name, string type, string config)
        {
            Name = name;
            Type = type;
            Config = config;
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        public void UpdateConfiguration(string config)
        {
            Config = config;
            UpdateTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 动作类型枚举
    /// </summary>
    public enum ActionType
    {
        MQTT,
        HTTP,
        Database,
        Email,
        SMS
    }
} 