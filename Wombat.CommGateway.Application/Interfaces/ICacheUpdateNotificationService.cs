using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 缓存更新通知服务接口
    /// 用于在缓存数据更新时通知WebSocket服务推送数据到前端
    /// </summary>
    public interface ICacheUpdateNotificationService
    {
        /// <summary>
        /// 通知单个点位数据更新
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="value">点位值</param>
        /// <param name="status">点位状态</param>
        /// <param name="updateTime">更新时间</param>
        /// <returns>通知处理结果</returns>
        Task OnPointDataUpdatedAsync(int pointId, string value, DataPointStatus status, System.DateTime updateTime);

        /// <summary>
        /// 通知批量点位数据更新
        /// </summary>
        /// <param name="updates">点位更新字典</param>
        /// <returns>通知处理结果</returns>
        Task OnBatchPointsDataUpdatedAsync(Dictionary<int, (string Value, DataPointStatus Status, System.DateTime UpdateTime)> updates);

        /// <summary>
        /// 通知点位状态变更
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="status">新状态</param>
        /// <returns>通知处理结果</returns>
        Task OnPointStatusChangedAsync(int pointId, DataPointStatus status);

        /// <summary>
        /// 通知点位被移除
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>通知处理结果</returns>
        Task OnPointRemovedAsync(int pointId);

        /// <summary>
        /// 通知批量点位被移除
        /// </summary>
        /// <param name="pointIds">点位ID列表</param>
        /// <returns>通知处理结果</returns>
        Task OnBatchPointsRemovedAsync(IEnumerable<int> pointIds);
    }

    /// <summary>
    /// 缓存更新事件参数
    /// </summary>
    public class CacheUpdateEventArgs
    {
        /// <summary>
        /// 点位ID
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 点位值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 点位状态
        /// </summary>
        public DataPointStatus Status { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public System.DateTime UpdateTime { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 点位名称
        /// </summary>
        public string PointName { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; }
    }

    /// <summary>
    /// 批量缓存更新事件参数
    /// </summary>
    public class BatchCacheUpdateEventArgs
    {
        /// <summary>
        /// 更新的点位列表
        /// </summary>
        public List<CacheUpdateEventArgs> Updates { get; set; } = new List<CacheUpdateEventArgs>();

        /// <summary>
        /// 更新时间
        /// </summary>
        public System.DateTime UpdateTime { get; set; }
    }
} 