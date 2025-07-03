using System;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 数据采集记录数据传输对象
    /// </summary>
    public class DataCollectionRecordDto
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
        /// 采集时间
        /// </summary>
        public DateTime CollectionTime { get; set; }

        /// <summary>
        /// 采集状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 采集质量
        /// </summary>
        public DataQuality Quality { get; set; }

        /// <summary>
        /// 采集时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 创建数据采集记录请求
    /// </summary>
    public class CreateDataCollectionRecordRequest
    {
        /// <summary>
        /// 关联的设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 关联的点位ID
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 采集值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 采集质量
        /// </summary>
        public DataQuality Quality { get; set; }
    }

    /// <summary>
    /// 数据采集记录查询请求
    /// </summary>
    public class QueryDataCollectionRecordRequest
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public int? DeviceId { get; set; }

        /// <summary>
        /// 点位ID
        /// </summary>
        public int? PointId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 数据质量
        /// </summary>
        public DataQuality? Quality { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
} 