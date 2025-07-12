using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 统一数据分发服务接口
    /// 负责将数据更新按订阅规则分发到不同的推送通道（SignalR、WebSocket等）
    /// </summary>
    public interface IDataDistributionService
    {
        /// <summary>
        /// 分发单个点位数据更新
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="value">点位值</param>
        /// <param name="status">点位状态</param>
        /// <param name="updateTime">更新时间</param>
        /// <returns></returns>
        Task DistributePointUpdateAsync(int pointId, string value, DataPointStatus status, DateTime updateTime);

        /// <summary>
        /// 分发批量点位数据更新
        /// </summary>
        /// <param name="updates">批量更新数据</param>
        /// <returns></returns>
        Task DistributeBatchPointsUpdateAsync(Dictionary<int, (string Value, DataPointStatus Status, DateTime UpdateTime)> updates);

        /// <summary>
        /// 分发点位状态变更
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="status">新状态</param>
        /// <returns></returns>
        Task DistributePointStatusChangeAsync(int pointId, DataPointStatus status);

        /// <summary>
        /// 分发点位移除通知
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns></returns>
        Task DistributePointRemovedAsync(int pointId);

        /// <summary>
        /// 分发批量点位移除通知
        /// </summary>
        /// <param name="pointIds">点位ID列表</param>
        /// <returns></returns>
        Task DistributeBatchPointsRemovedAsync(IEnumerable<int> pointIds);

        /// <summary>
        /// 获取分发统计信息
        /// </summary>
        /// <returns></returns>
        Task<DistributionStatistics> GetDistributionStatisticsAsync();
    }

    /// <summary>
    /// 数据分发统计信息
    /// </summary>
    public class DistributionStatistics
    {
        /// <summary>
        /// SignalR连接数
        /// </summary>
        public int SignalRConnections { get; set; }

        /// <summary>
        /// WebSocket连接数
        /// </summary>
        public int WebSocketConnections { get; set; }

        /// <summary>
        /// 总订阅数
        /// </summary>
        public int TotalSubscriptions { get; set; }

        /// <summary>
        /// 分发的消息总数
        /// </summary>
        public long TotalDistributedMessages { get; set; }

        /// <summary>
        /// 最后分发时间
        /// </summary>
        public DateTime LastDistributionTime { get; set; }

        /// <summary>
        /// 分发渠道详情
        /// </summary>
        public Dictionary<string, int> ChannelDetails { get; set; } = new();
    }
} 