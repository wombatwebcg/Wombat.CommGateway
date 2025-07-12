using Wombat.CommGateway.Domain.Enums;
using Wombat.CommGateway.Application.Common.Logging;

namespace Wombat.CommGateway.Application.Services.Logging;

/// <summary>
/// 日志订阅管理器接口
/// </summary>
public interface ILogSubscriptionManager : IDisposable
{
    #region 类别订阅
    /// <summary>
    /// 订阅日志类别
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="category">日志类别</param>
    Task SubscribeToCategoryAsync(string connectionId, LogCategory category);

    /// <summary>
    /// 取消订阅日志类别
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="category">日志类别</param>
    Task UnsubscribeFromCategoryAsync(string connectionId, LogCategory category);

    /// <summary>
    /// 获取连接的类别订阅
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>类别列表</returns>
    IReadOnlyList<LogCategory> GetCategorySubscriptions(string connectionId);

    /// <summary>
    /// 获取订阅指定类别的连接
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <returns>连接ID列表</returns>
    IReadOnlyList<string> GetCategorySubscribers(LogCategory category);
    #endregion

    #region 级别订阅
    /// <summary>
    /// 订阅日志级别
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="level">日志级别</param>
    Task SubscribeToLevelAsync(string connectionId, LogLevel level);

    /// <summary>
    /// 取消订阅日志级别
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <param name="level">日志级别</param>
    Task UnsubscribeFromLevelAsync(string connectionId, LogLevel level);

    /// <summary>
    /// 获取连接的级别订阅
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>级别列表</returns>
    IReadOnlyList<LogLevel> GetLevelSubscriptions(string connectionId);

    /// <summary>
    /// 获取订阅指定级别的连接
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <returns>连接ID列表</returns>
    IReadOnlyList<string> GetLevelSubscribers(LogLevel level);
    #endregion

    #region 连接管理
    /// <summary>
    /// 清理连接的所有订阅
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    Task ClearConnectionSubscriptionsAsync(string connectionId);

    /// <summary>
    /// 获取所有连接ID
    /// </summary>
    /// <returns>连接ID列表</returns>
    IReadOnlyList<string> GetAllConnections();

    /// <summary>
    /// 获取连接的订阅统计信息
    /// </summary>
    /// <param name="connectionId">连接ID</param>
    /// <returns>统计信息</returns>
    Task<LogSubscriptionStats> GetConnectionStatsAsync(string connectionId);
    #endregion

    #region 日志推送
    /// <summary>
    /// 获取应该接收指定日志的连接
    /// </summary>
    /// <param name="category">日志类别</param>
    /// <param name="level">日志级别</param>
    /// <returns>连接ID列表</returns>
    IReadOnlyList<string> GetConnectionsForLog(LogCategory category, LogLevel level);
    #endregion
}

/// <summary>
/// 日志订阅统计信息
/// </summary>
public class LogSubscriptionStats
{
    /// <summary>
    /// 连接ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// 订阅的类别
    /// </summary>
    public IReadOnlyList<LogCategory> Categories { get; set; } = Array.Empty<LogCategory>();

    /// <summary>
    /// 订阅的级别
    /// </summary>
    public IReadOnlyList<LogLevel> Levels { get; set; } = Array.Empty<LogLevel>();

    /// <summary>
    /// 总订阅数
    /// </summary>
    public int TotalSubscriptions => Categories.Count + Levels.Count;

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivityTime { get; set; }
} 