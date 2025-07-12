using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Wombat.CommGateway.Application.Services.Logging;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;
using DomainLogLevel = Wombat.CommGateway.Domain.Enums.LogLevel;

namespace Wombat.CommGateway.Application.Hubs;

/// <summary>
/// 日志推送Hub
/// 提供实时日志推送功能，支持按日志类别订阅
/// </summary>
public class LoggingHub : Hub
{
    private readonly ILogger<LoggingHub> _logger;
    private readonly ILogSubscriptionManager _logSubscriptionManager;

    public LoggingHub(
        ILogger<LoggingHub> logger,
        ILogSubscriptionManager logSubscriptionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logSubscriptionManager = logSubscriptionManager ?? throw new ArgumentNullException(nameof(logSubscriptionManager));
    }

    /// <summary>
    /// 连接建立时的处理
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("日志客户端连接: {ConnectionId}", connectionId);
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 连接断开时的处理
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        // 清理所有订阅
        await _logSubscriptionManager.ClearConnectionSubscriptionsAsync(connectionId);
        
        _logger.LogInformation("日志客户端断开连接: {ConnectionId}", connectionId);
        
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 订阅系统日志
    /// </summary>
    [HubMethodName("SubscribeSystemLogs")]
    public async Task SubscribeSystemLogsAsync()
    {
        var connectionId = Context.ConnectionId;
        await _logSubscriptionManager.SubscribeToCategoryAsync(connectionId, LogCategory.System);
        
        _logger.LogInformation("客户端 {ConnectionId} 订阅系统日志", connectionId);
    }

    /// <summary>
    /// 订阅操作日志
    /// </summary>
    [HubMethodName("SubscribeOperationLogs")]
    public async Task SubscribeOperationLogsAsync()
    {
        var connectionId = Context.ConnectionId;
        await _logSubscriptionManager.SubscribeToCategoryAsync(connectionId, LogCategory.Operation);
        
        _logger.LogInformation("客户端 {ConnectionId} 订阅操作日志", connectionId);
    }

    /// <summary>
    /// 订阅通信日志
    /// </summary>
    [HubMethodName("SubscribeCommunicationLogs")]
    public async Task SubscribeCommunicationLogsAsync()
    {
        var connectionId = Context.ConnectionId;
        await _logSubscriptionManager.SubscribeToCategoryAsync(connectionId, LogCategory.Communication);
        
        _logger.LogInformation("客户端 {ConnectionId} 订阅通信日志", connectionId);
    }

    /// <summary>
    /// 订阅特定级别的日志
    /// </summary>
    [HubMethodName("SubscribeLogLevel")]
    public async Task SubscribeLogLevelAsync(DomainLogLevel logLevel)
    {
        var connectionId = Context.ConnectionId;
        await _logSubscriptionManager.SubscribeToLevelAsync(connectionId, logLevel);
        
        _logger.LogInformation("客户端 {ConnectionId} 订阅日志级别: {LogLevel}", connectionId, logLevel);
    }

    /// <summary>
    /// 取消订阅系统日志
    /// </summary>
    [HubMethodName("UnsubscribeSystemLogs")]
    public async Task UnsubscribeSystemLogsAsync()
    {
        var connectionId = Context.ConnectionId;
        await _logSubscriptionManager.UnsubscribeFromCategoryAsync(connectionId, LogCategory.System);
        
        _logger.LogInformation("客户端 {ConnectionId} 取消订阅系统日志", connectionId);
    }

    /// <summary>
    /// 取消订阅操作日志
    /// </summary>
    [HubMethodName("UnsubscribeOperationLogs")]
    public async Task UnsubscribeOperationLogsAsync()
    {
        var connectionId = Context.ConnectionId;
        await _logSubscriptionManager.UnsubscribeFromCategoryAsync(connectionId, LogCategory.Operation);
        
        _logger.LogInformation("客户端 {ConnectionId} 取消订阅操作日志", connectionId);
    }

    /// <summary>
    /// 取消订阅通信日志
    /// </summary>
    [HubMethodName("UnsubscribeCommunicationLogs")]
    public async Task UnsubscribeCommunicationLogsAsync()
    {
        var connectionId = Context.ConnectionId;
        await _logSubscriptionManager.UnsubscribeFromCategoryAsync(connectionId, LogCategory.Communication);
        
        _logger.LogInformation("客户端 {ConnectionId} 取消订阅通信日志", connectionId);
    }

    /// <summary>
    /// 取消订阅特定级别的日志
    /// </summary>
    [HubMethodName("UnsubscribeLogLevel")]
    public async Task UnsubscribeLogLevelAsync(DomainLogLevel logLevel)
    {
        var connectionId = Context.ConnectionId;
        await _logSubscriptionManager.UnsubscribeFromLevelAsync(connectionId, logLevel);
        
        _logger.LogInformation("客户端 {ConnectionId} 取消订阅日志级别: {LogLevel}", connectionId, logLevel);
    }

    /// <summary>
    /// 获取订阅统计信息
    /// </summary>
    [HubMethodName("GetSubscriptionStats")]
    public async Task<object> GetSubscriptionStatsAsync()
    {
        var connectionId = Context.ConnectionId;
        var stats = await _logSubscriptionManager.GetConnectionStatsAsync(connectionId);
        
        return new
        {
            ConnectionId = connectionId,
            Categories = stats.Categories,
            Levels = stats.Levels,
            TotalSubscriptions = stats.TotalSubscriptions
        };
    }
} 