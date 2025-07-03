using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Wombat.CommGateway.Domain.Common;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 网关设备实体
    /// </summary>
    /// 

    [Table("GatewayDevices")]

    public class GatewayDevice : AggregateRoot
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
        /// 设备状态
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 设备属性
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// 设备点位列表
        /// </summary>
        public List<DevicePoint> Points { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        private GatewayDevice() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">设备名称</param>
        /// <param name="deviceType">设备类型</param>
        /// <param name="address">设备地址</param>
        public GatewayDevice(string name, DeviceType deviceType, string address)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("设备名称不能为空", nameof(name));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("设备地址不能为空", nameof(address));

            Name = name;
            Type = deviceType;
            Address = address;
            IsEnabled = false;
            Properties = new Dictionary<string, string>();
            Points = new List<DevicePoint>();
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新设备名称
        /// </summary>
        /// <param name="name">设备名称</param>
        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("设备名称不能为空", nameof(name));

            Name = name;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新设备类型
        /// </summary>
        /// <param name="type">设备类型</param>
        public void UpdateDeviceType(DeviceType type)
        {
            Type = type;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新设备地址
        /// </summary>
        /// <param name="address">设备地址</param>
        public void UpdateAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("设备地址不能为空", nameof(address));

            Address = address;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新通信通道ID
        /// </summary>
        /// <param name="channelId">通信通道ID</param>
        public void UpdateChannelId(int channelId)
        {
            if (channelId <= 0)
                throw new ArgumentException("通信通道ID必须大于0", nameof(channelId));

            ChannelId = channelId;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新协议配置ID
        /// </summary>
        /// <param name="protocolConfigId">协议配置ID</param>
        public void UpdateProtocolConfigId(int protocolConfigId)
        {
            if (protocolConfigId <= 0)
                throw new ArgumentException("协议配置ID必须大于0", nameof(protocolConfigId));

            ProtocolConfigId = protocolConfigId;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新设备状态
        /// </summary>
        /// <param name="status">设备状态</param>
        public void UpdateStatus(DeviceStatus status)
        {
            IsEnabled = status == DeviceStatus.Running;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 添加点位
        /// </summary>
        /// <param name="point">点位</param>
        public void AddPoint(DevicePoint point)
        {
            if (point == null)
                throw new ArgumentNullException(nameof(point));

            Points.Add(point);
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 移除点位
        /// </summary>
        /// <param name="pointId">点位ID</param>
        public void RemovePoint(int pointId)
        {
            Points.RemoveAll(p => p.Id == pointId);
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新设备属性
        /// </summary>
        /// <param name="properties">设备属性</param>
        public void UpdateProperties(Dictionary<string, string> properties)
        {
            Properties = properties ?? new Dictionary<string, string>();
            UpdateTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 设备类型枚举
    /// </summary>
    public enum DeviceType
    {
        /// <summary>
        /// 串口设备
        /// </summary>
        Serial,

        /// <summary>
        /// 以太网设备
        /// </summary>
        Ethernet,

        /// <summary>
        /// CAN设备
        /// </summary>
        CAN,

        /// <summary>
        /// PROFINET设备
        /// </summary>
        PROFINET
    }
} 