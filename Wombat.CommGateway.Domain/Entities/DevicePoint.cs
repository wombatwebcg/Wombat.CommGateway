using System;
using System.Collections.Generic;
using Wombat.Extensions.DataTypeExtensions;
using Wombat.CommGateway.Domain.Common;
using Wombat.CommGateway.Domain.Enums;
using FreeSql.DataAnnotations;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 设备点位实体
    /// </summary>
    /// 

    [Table(Name ="DevicePoints")]

    public class DevicePoint : Entity
    {

        public string Name { get; set; }

        public string Address { get; set; }

        public int ScanRate { get; set; }


        [JsonMap]
        public Dictionary<string, string> Properties { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }


        public int DeviceId { get; set; }


        [Navigate(nameof(DeviceId))]
        public Device Device { get; set; }

        public DataType DataType { get; set; }

        public ReadWriteType ReadWrite { get; set; }

        public bool Enable { get; set; }

        public DataPointStatus Status { get; set; }

        public string Remark { get; set; }

        public string Value { get; set; }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private DevicePoint() 
        {
            Properties = new Dictionary<string, string>();
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">点位名称</param>
        /// <param name="deviceId">设备ID</param>
        /// <param name="deviceGroupId">设备组ID</param>
        /// <param name="address">点位地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="readWrite">读写类型</param>
        /// <param name="scanRate">扫描周期</param>
        /// <param name="enable">是否启用</param>
        /// <param name="remark">备注</param>
        public DevicePoint(string name, int deviceId, string address, DataType dataType, ReadWriteType readWrite, int scanRate, bool enable = true, string remark = "")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("点位名称不能为空", nameof(name));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("点位地址不能为空", nameof(address));
            if (scanRate <= 0)
                throw new ArgumentException("扫描周期必须大于0", nameof(scanRate));

            Name = name;
            DeviceId = deviceId;
            Address = address;
            DataType = dataType;
            ReadWrite = readWrite;
            ScanRate = scanRate;
            Enable = enable;
            Status = DataPointStatus.Unknown;
            Remark = remark ?? "";
            Properties = new Dictionary<string, string>();
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新点位名称
        /// </summary>
        /// <param name="name">点位名称</param>
        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("点位名称不能为空", nameof(name));

            Name = name;
            UpdateTime = DateTime.Now;
        }




        /// <summary>
        /// 更新点位属性
        /// </summary>
        /// <param name="properties">点位属性</param>
        public void UpdateProperties(Dictionary<string, string> properties)
        {
            Properties = properties ?? new Dictionary<string, string>();
            UpdateTime = DateTime.Now;
        }
    }


} 