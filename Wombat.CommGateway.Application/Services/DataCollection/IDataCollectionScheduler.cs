using System;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    /// <summary>
    /// 数据采集调度器接口
    /// 负责管理点位的采集调度，实现KepServer风格的多周期采样
    /// </summary>
    public interface IDataCollectionScheduler
    {
        /// <summary>
        /// 注册点位到调度器
        /// </summary>
        /// <param name="point">设备点位</param>
        /// <returns>注册结果</returns>
        Task<bool> RegisterPointAsync(DevicePoint point);

        /// <summary>
        /// 从调度器注销点位
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>注销结果</returns>
        Task<bool> UnregisterPointAsync(int pointId);

        /// <summary>
        /// 启动调度器
        /// </summary>
        /// <returns>启动结果</returns>
        Task StartAsync();

        /// <summary>
        /// 停止调度器
        /// </summary>
        /// <returns>停止结果</returns>
        Task StopAsync();

        /// <summary>
        /// 更新点位的扫描周期
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="newScanRate">新的扫描周期（毫秒）</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdatePointScheduleAsync(int pointId, int newScanRate);

        /// <summary>
        /// 获取调度器状态
        /// </summary>
        /// <returns>调度器运行状态</returns>
        bool IsRunning { get; }

        /// <summary>
        /// 获取当前调度的点位数量
        /// </summary>
        /// <returns>调度中的点位数量</returns>
        int ScheduledPointCount { get; }

        /// <summary>
        /// 获取调度统计信息
        /// </summary>
        /// <returns>调度统计信息</returns>
        SchedulerStatistics GetStatistics();
    }

    /// <summary>
    /// 调度器统计信息
    /// </summary>
    public class SchedulerStatistics
    {
        /// <summary>
        /// 总调度任务数
        /// </summary>
        public long TotalScheduledTasks { get; set; }

        /// <summary>
        /// 成功执行的任务数
        /// </summary>
        public long SuccessfulTasks { get; set; }

        /// <summary>
        /// 失败的任务数
        /// </summary>
        public long FailedTasks { get; set; }

        /// <summary>
        /// 平均执行时间（毫秒）
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// 最后执行时间
        /// </summary>
        public DateTime LastExecutionTime { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => TotalScheduledTasks > 0 ? (double)SuccessfulTasks / TotalScheduledTasks : 0;
    }
} 