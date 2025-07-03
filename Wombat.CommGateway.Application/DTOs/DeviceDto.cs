using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 设备数据传输对象
    /// </summary>
    public class DeviceDto
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
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 通信通道ID
        /// </summary>
        public int ChannelId { get; set; }


        /// <summary>
        /// 通信通道ID
        /// </summary>
        public string ChannelName { get; set; }


        /// <summary>
        /// 设备组ID
        /// </summary>
        public string DeviceGroupName { get; set; }


        /// <summary>
        /// 设备组ID
        /// </summary>
        public int DeviceGroupId { get; set; }

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
    /// 设备查询数据传输对象
    /// </summary>
    public class DeviceQueryDto
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string Name { get; set; }

    }

    /// <summary>
    /// 设备响应数据传输对象
    /// </summary>
    public class DeviceResponseDto
    {
        public List<DeviceDto> Items { get; set; }
        public int Total { get; set; }
    }

    /// <summary>
    /// 创建设备数据传输对象
    /// </summary>
    public class CreateDeviceDto
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
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }

        public int ChannelId { get; set; }

        public string ChannelName { get; set; }

        public string DeviceGroupName { get; set; }

        /// <summary>
        /// 设备组ID
        /// </summary>
        public int DeviceGroupId { get; set; }
    }

    /// <summary>
    /// 更新设备数据传输对象
    /// </summary>
    public class UpdateDeviceDto
    {

        public string Name { get; set; }


        public string Description { get; set; }




        public bool Enable { get; set; }




        public int ChannelId { get; set; }




        public int DeviceGroupId { get; set; }


        public string ChannelName { get; set; }

        public string DeviceGroupName
        {
            get; set;
        }

    }

    /// <summary>
    /// 更新设备启用状态数据传输对象
    /// </summary>
    public class UpdateDeviceEnableDto
    {
        public bool Enable { get; set; }
    }
} 