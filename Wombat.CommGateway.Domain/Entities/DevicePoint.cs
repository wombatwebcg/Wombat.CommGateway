using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Wombat.Extensions.DataTypeExtensions;
using Wombat.CommGateway.Domain.Common;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 设备点位实体
    /// </summary>
    /// 

    [Table("DevicePoints")]

    public class DevicePoint : Entity
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
        public DataTypeEnums DataType { get; set; }

        /// <summary>
        /// 扫描周期(毫秒)
        /// </summary>
        public int ScanRate { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

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
        public DateTime UpdateTime { get; set; }


        private DevicePoint() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">点位名称</param>
        /// <param name="address">点位地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="scanRate">扫描周期</param>
        public DevicePoint(string name, string address, DataTypeEnums dataType, int scanRate)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("点位名称不能为空", nameof(name));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("点位地址不能为空", nameof(address));
            if (scanRate <= 0)
                throw new ArgumentException("扫描周期必须大于0", nameof(scanRate));

            Name = name;
            Address = address;
            DataType = dataType;
            ScanRate = scanRate;
            IsEnabled = true;
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
        /// 更新点位地址
        /// </summary>
        /// <param name="address">点位地址</param>
        public void UpdateAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("点位地址不能为空", nameof(address));

            Address = address;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新数据类型
        /// </summary>
        /// <param name="dataType">数据类型</param>
        public void UpdateDataType(DataTypeEnums dataType)
        {
            DataType = dataType;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新扫描周期
        /// </summary>
        /// <param name="scanRate">扫描周期</param>
        public void UpdateScanRate(int scanRate)
        {
            if (scanRate <= 0)
                throw new ArgumentException("扫描周期必须大于0", nameof(scanRate));

            ScanRate = scanRate;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 更新点位状态
        /// </summary>
        /// <param name="isEnabled">是否启用</param>
        public void UpdateStatus(bool isEnabled)
        {
            IsEnabled = isEnabled;
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