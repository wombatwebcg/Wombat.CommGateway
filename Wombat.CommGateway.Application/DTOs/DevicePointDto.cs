using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 设备点位数据传输对象
    /// </summary>
    public class DevicePointDto
    {
        /// <summary>
        /// 点位ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 点位名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 点位地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 扫描周期（毫秒）
        /// </summary>
        public int ScanRate { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 点位属性
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

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
    /// 创建设备点位数据传输对象
    /// </summary>
    public class CreateDevicePointDto
    {

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 点位名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 点位地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 扫描周期（毫秒）
        /// </summary>
        public int ScanRate { get; set; }

        /// <summary>
        /// 点位属性
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }

    /// <summary>
    /// 更新设备点位数据传输对象
    /// </summary>
    public class UpdateDevicePointDto
    {
        /// <summary>
        /// 点位名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 点位地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 扫描周期（毫秒）
        /// </summary>
        public int ScanRate { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// 点位属性
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }
} 