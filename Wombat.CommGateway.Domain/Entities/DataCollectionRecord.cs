using System;
using System.ComponentModel.DataAnnotations.Schema;
using Wombat.CommGateway.Domain.Common;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 数据采集记录实体
    /// </summary>
    /// 
    [Table("DataCollectionRecords")]
    public class DataCollectionRecord : Entity
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 点位ID
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 采集值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 采集时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 质量戳
        /// </summary>
        public string Quality { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        public DataCollectionRecord() { }

        public DataCollectionRecord(int deviceId, int pointId, string value, string quality)
        {
            DeviceId = deviceId;
            PointId = pointId;
            Value = value;
            Quality = quality;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 数据质量枚举
    /// </summary>
    public enum DataQuality
    {
        Good,
        Bad,
        Uncertain
    }
} 