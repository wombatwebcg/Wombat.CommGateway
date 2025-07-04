using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Wombat.CommGateway.Domain.Common;
using TableAttribute = FreeSql.DataAnnotations.TableAttribute;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 通信通道实体
    /// </summary>
    /// 

    [Table(Name ="Channels")]
    public class Channel : Entity
    {
        /// <summary>
        /// 通道名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 通道类型
        /// </summary>
        public ChannelType Type { get; set; }

        /// <summary>
        /// 通道状态
        /// </summary>
        public ChannelStatus Status { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 协议类型
        /// </summary>
        public ProtocolType Protocol { get; set; }

        /// <summary>
        /// 通道配置
        /// </summary>
        /// 
        [JsonMap]
        public Dictionary<string, string> Configuration { get; set; }

        /// <summary>
        /// 关联的协议配置ID
        /// </summary>
        /// 
        [Navigate(nameof(ProtocolConfig.Id))]
        public int ProtocolConfigId { get; set; }

        /// <summary>
        /// 关联的协议配置
        /// </summary>
        public ProtocolConfig ProtocolConfig { get; set; }


        public ChannelRole Role { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }



        private Channel() { }

        public Channel(string name, ChannelType type, ProtocolType protocol, ChannelRole role, int protocolConfigId, bool enable = true)
        {
            Name = name;
            Type = type;
            Protocol = protocol;
            Role = role;
            ProtocolConfigId = protocolConfigId;
            Enable = enable;
            Status = ChannelStatus.Created;
            Configuration = new Dictionary<string, string>();
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        public void UpdateStatus(ChannelStatus status)
        {
            Status = status;
            UpdateTime = DateTime.Now;
        }

        public void UpdateConfiguration(Dictionary<string, string> configuration)
        {
            Configuration = configuration;
            UpdateTime = DateTime.Now;
        }

        public void UpdateEnable(bool enable)
        {
            Enable = enable;
            UpdateTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 通道类型枚举
    /// </summary>
    public enum ChannelType
    {
        Ethernet =1,
        Serial = 2,
        CAN =3,
        PROFINET = 4
    }

    /// <summary>
    /// 通道状态枚举
    /// </summary>
    public enum ChannelStatus
    {
        Created = 0,
        Running = 1,
        Stopped = 2,
        Error = 3
    }

    public enum ChannelRole
    {
        Client = 1,
        Server = 2
    }
}  