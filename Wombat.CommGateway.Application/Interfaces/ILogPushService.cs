using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 日志推送服务接口
    /// </summary>
    public interface ILogPushService
    {
        /// <summary>
        /// 推送系统日志
        /// </summary>
        /// <param name="log">系统日志</param>
        Task PushSystemLogAsync(SystemLog log);

        /// <summary>
        /// 推送操作日志
        /// </summary>
        /// <param name="log">操作日志</param>
        Task PushOperationLogAsync(OperationLog log);

        /// <summary>
        /// 推送通信日志
        /// </summary>
        /// <param name="log">通信日志</param>
        Task PushCommunicationLogAsync(CommunicationLog log);

        /// <summary>
        /// 批量推送日志
        /// </summary>
        /// <param name="logs">日志列表</param>
        Task PushLogsAsync(List<object> logs);

        /// <summary>
        /// 推送日志统计信息
        /// </summary>
        /// <param name="statistics">统计信息</param>
        Task PushLogStatisticsAsync(object statistics);

        /// <summary>
        /// 订阅日志推送
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="filters">过滤条件</param>
        Task SubscribeAsync(string connectionId, LogSubscriptionFilter filters);

        /// <summary>
        /// 取消订阅日志推送
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        Task UnsubscribeAsync(string connectionId);

        /// <summary>
        /// 获取当前连接数
        /// </summary>
        Task<int> GetConnectionCountAsync();

        /// <summary>
        /// 获取活跃连接列表
        /// </summary>
        Task<List<string>> GetActiveConnectionsAsync();
    }

    /// <summary>
    /// 日志订阅过滤条件
    /// </summary>
    public class LogSubscriptionFilter
    {
        /// <summary>
        /// 日志级别过滤
        /// </summary>
        public List<LogLevel> Levels { get; set; } = new List<LogLevel>();

        /// <summary>
        /// 日志分类过滤
        /// </summary>
        public List<LogCategory> Categories { get; set; } = new List<LogCategory>();

        /// <summary>
        /// 是否包含系统日志
        /// </summary>
        public bool IncludeSystemLogs { get; set; } = true;

        /// <summary>
        /// 是否包含操作日志
        /// </summary>
        public bool IncludeOperationLogs { get; set; } = true;

        /// <summary>
        /// 是否包含通信日志
        /// </summary>
        public bool IncludeCommunicationLogs { get; set; } = true;

        /// <summary>
        /// 关键字过滤
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// 用户ID过滤（仅操作日志）
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// 设备ID过滤（仅通信日志）
        /// </summary>
        public int? DeviceId { get; set; }

        /// <summary>
        /// 通道ID过滤（仅通信日志）
        /// </summary>
        public int? ChannelId { get; set; }

        /// <summary>
        /// 是否启用过滤
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 检查日志是否匹配过滤条件
        /// </summary>
        /// <param name="log">日志对象</param>
        /// <returns>是否匹配</returns>
        public bool Matches(object log)
        {
            if (!IsEnabled)
                return true;

            switch (log)
            {
                case SystemLog systemLog:
                    return MatchesSystemLog(systemLog);
                case OperationLog operationLog:
                    return MatchesOperationLog(operationLog);
                case CommunicationLog communicationLog:
                    return MatchesCommunicationLog(communicationLog);
                default:
                    return false;
            }
        }

        private bool MatchesSystemLog(SystemLog log)
        {
            if (!IncludeSystemLogs)
                return false;

            if (Levels.Count > 0 && !Levels.Contains(log.Level))
                return false;

            if (Categories.Count > 0 && !Categories.Contains(log.Category))
                return false;

            if (!string.IsNullOrWhiteSpace(Keyword) && 
                !log.Message.Contains(Keyword, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private bool MatchesOperationLog(OperationLog log)
        {
            if (!IncludeOperationLogs)
                return false;

            if (UserId.HasValue && log.UserId != UserId.Value)
                return false;

            if (!string.IsNullOrWhiteSpace(Keyword) && 
                !log.Description.Contains(Keyword, StringComparison.OrdinalIgnoreCase) &&
                !log.Action.Contains(Keyword, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private bool MatchesCommunicationLog(CommunicationLog log)
        {
            if (!IncludeCommunicationLogs)
                return false;

            if (DeviceId.HasValue && log.DeviceId != DeviceId.Value)
                return false;

            if (ChannelId.HasValue && log.ChannelId != ChannelId.Value)
                return false;

            if (!string.IsNullOrWhiteSpace(Keyword) && 
                !log.Protocol.Contains(Keyword, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
    }
} 