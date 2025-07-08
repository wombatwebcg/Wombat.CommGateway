using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 设备点位数据传输对象
    /// </summary>
    public class DevicePointDto
    {

        public int Id { get; set; }


        public string Name { get; set; }


        public int DeviceGroupId { get; set; }


        public int DeviceId { get; set; }


        public string DeviceName { get; set; }


        public string Address { get; set; }


        public DataType DataType { get; set; }


        public ReadWriteType ReadWrite { get; set; }


        public int ScanRate { get; set; }


        public bool Enable { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public string CreateTime { get; set; }

        public string UpdateTime { get; set; }

        public DataPointStatus Status { get; set; }


        public string Remark { get; set; }
        public string? Value { get; set; }


    }

    /// <summary>
    /// 创建设备点位数据传输对象
    /// </summary>
    public class CreateDevicePointDto
    {
        public string Name { get; set; }
        public int DeviceGroupId { get; set; }
        public int DeviceId { get; set; }
        public string Address { get; set; }
        public DataType DataType { get; set; }
        public ReadWriteType ReadWrite { get; set; }
        public int ScanRate { get; set; }
        public bool Enable { get; set; }
        public string Remark { get; set; }
        public Dictionary<string, string>? Properties { get; set; }
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
        public bool? Enable { get; set; }

        /// <summary>
        /// 点位属性
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }

    public class PointQueryDto
    {
        public int? DeviceId { get; set; }
        public int? DataType { get; set; }
        public int? Status { get; set; }
        public int? GroupId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class PointListResponseDto
    {
        public List<DevicePointDto> Items { get; set; }
        public int Total { get; set; }
    }

    public class UpdateDevicePointEnableDto
    {
        public bool Enable { get; set; }
    }
} 