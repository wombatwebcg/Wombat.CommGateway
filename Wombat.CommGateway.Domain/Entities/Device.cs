using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Common;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 网关设备实体
    /// </summary>
    /// 

    [Table(Name ="Devices")]

    public class Device : AggregateRoot
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
        /// 设备状态
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 设备属性
        /// </summary>
        /// 
        [JsonMap]
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// 所属设备组
        /// </summary>
        [Navigate(nameof(DeviceGroupId))]
        public DeviceGroup DeviceGroup { get; set; }


        [Navigate(nameof(DeviceGroup.Id))]
        public int DeviceGroupId { get; set; }


        [Navigate(nameof(ChannelId))]
        public Channel Channel { get; set; }


        [Navigate(nameof(Channel.Id))]
        public int ChannelId { get; set; }


        /// <summary>
        /// 设备点位列表
        /// </summary>
        /// 
        [Navigate(nameof(DevicePoint.DeviceId))]
        public List<DevicePoint>? Points { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }



        private Device() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">设备名称</param>
        /// <param name="deviceType">设备类型</param>
        /// <param name="address">设备地址</param>
        public Device(string name, int deviceGroupId, int channelId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("设备名称不能为空", nameof(name));


            Name = name;
            DeviceGroupId = deviceGroupId;
            ChannelId = channelId;
            Enable = true;
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
        /// 更新设备状态
        /// </summary>
        /// <param name="status">设备状态</param>
        public void UpdateEnable(bool enable)
        {
            Enable = enable;
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


} 