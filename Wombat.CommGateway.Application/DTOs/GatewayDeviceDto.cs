using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 网关设备数据传输对象
    /// </summary>
    public class GatewayDeviceDto
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 设备描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public DeviceType Type { get; set; }

        /// <summary>
        /// 设备地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 通信通道ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 协议配置ID
        /// </summary>
        public int ProtocolConfigId { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 设备属性
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// 设备点位列表
        /// </summary>
        public List<DevicePointDto> Points { get; set; }

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
    /// 创建网关设备数据传输对象
    /// </summary>
    public class CreateGatewayDeviceDto
    {
        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 设备描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public DeviceType Type { get; set; }

        /// <summary>
        /// 设备地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 通信通道ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 协议配置ID
        /// </summary>
        public int ProtocolConfigId { get; set; }

        /// <summary>
        /// 设备属性
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }

    /// <summary>
    /// 更新设备DTO
    /// </summary>
    public class UpdateGatewayDeviceDto
    {
        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 设备描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        public DeviceType Type { get; set; }

        /// <summary>
        /// 设备地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 通信通道ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 协议配置ID
        /// </summary>
        public int ProtocolConfigId { get; set; }

        /// <summary>
        /// 设备属性
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }
} 