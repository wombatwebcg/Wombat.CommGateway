using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Services.DataCollection.Models
{
    /// <summary>
    /// 数据采集任务
    /// </summary>
    public class CollectionTask
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public Guid TaskId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 任务优先级（数字越小优先级越高）
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 目标通道ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 目标设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 要采集的点位列表
        /// </summary>
        public List<CollectionPoint> Points { get; set; } = new List<CollectionPoint>();

        /// <summary>
        /// 任务创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 计划执行时间
        /// </summary>
        public DateTime ScheduledTime { get; set; }

        /// <summary>
        /// 实际执行时间
        /// </summary>
        public DateTime? ExecutedTime { get; set; }

        /// <summary>
        /// 任务状态
        /// </summary>
        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 任务执行结果
        /// </summary>
        public TaskResult Result { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 任务执行耗时（毫秒）
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// 采集点位信息
    /// </summary>
    public class CollectionPoint
    {
        /// <summary>
        /// 点位ID
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 点位地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// 扫描周期（毫秒）
        /// </summary>
        public int ScanRate { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; } = true;


        public ReadWriteType ReadWrite{ get; set; }

    }

    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// 等待执行
        /// </summary>
        Pending = 0,

        /// <summary>
        /// 正在执行
        /// </summary>
        Running = 1,

        /// <summary>
        /// 执行成功
        /// </summary>
        Completed = 2,

        /// <summary>
        /// 执行失败
        /// </summary>
        Failed = 3,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 4
    }

    /// <summary>
    /// 任务结果枚举
    /// </summary>
    public enum TaskResult
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,

        /// <summary>
        /// 部分成功
        /// </summary>
        PartialSuccess = 1,

        /// <summary>
        /// 失败
        /// </summary>
        Failure = 2,

        /// <summary>
        /// 超时
        /// </summary>
        Timeout = 3,

        /// <summary>
        /// 连接错误
        /// </summary>
        ConnectionError = 4,

        /// <summary>
        /// 数据错误
        /// </summary>
        DataError = 5
    }
} 