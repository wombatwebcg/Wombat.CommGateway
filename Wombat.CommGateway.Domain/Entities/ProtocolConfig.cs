using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Wombat.CommGateway.Domain.Common;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 协议配置实体
    /// </summary>
    /// 

    [Table("ProtocolConfigs")]
    public class ProtocolConfig : Entity
    {
        /// <summary>
        /// 协议名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 协议类型
        /// </summary>
        public ProtocolType Type { get; set; }

        /// <summary>
        /// 协议版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 协议参数
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        private ProtocolConfig() { }

        public ProtocolConfig(string name, ProtocolType type, string version)
        {
            Name = name;
            Type = type;
            Version = version;
            IsEnabled = true;
            Parameters = new Dictionary<string, string>();
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        public void UpdateStatus(bool isEnabled)
        {
            IsEnabled = isEnabled;
            UpdateTime = DateTime.Now;
        }

        public void UpdateParameters(Dictionary<string, string> parameters)
        {
            Parameters = parameters;
            UpdateTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 协议类型枚举
    /// </summary>
    public enum ProtocolType
    {
        ModbusTCP = 0,
        ModbusRTU = 1,
        SiemensS7 = 2,
        MitsubishiMC = 3,
        OmronFINS = 4
    }
} 