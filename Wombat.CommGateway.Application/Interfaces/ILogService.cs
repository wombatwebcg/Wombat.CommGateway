using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILogService
    {
        #region 系统日志

        /// <summary>
        /// 记录系统日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="category">日志分类</param>
        /// <param name="source">日志来源</param>
        /// <param name="userId">用户ID</param>
        /// <param name="exception">异常信息</param>
        /// <param name="properties">扩展属性</param>
        Task LogSystemAsync(LogLevel level, string message, LogCategory category, string source = null, 
            int? userId = null, Exception exception = null, Dictionary<string, object> properties = null);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        Task LogInformationAsync(string message, LogCategory category = LogCategory.System, string source = null);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        Task LogWarningAsync(string message, LogCategory category = LogCategory.System, string source = null);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        Task LogErrorAsync(string message, Exception exception = null, LogCategory category = LogCategory.System, string source = null);

        /// <summary>
        /// 记录调试日志
        /// </summary>
        Task LogDebugAsync(string message, LogCategory category = LogCategory.System, string source = null);

        /// <summary>
        /// 获取系统日志分页列表
        /// </summary>
        Task<(List<SystemLog> items, int totalCount)> GetSystemLogsPagedAsync(
            int page, int pageSize, 
            LogLevel? level = null, 
            LogCategory? category = null, 
            DateTime? startTime = null, 
            DateTime? endTime = null,
            string keyword = null);

        /// <summary>
        /// 获取系统日志统计
        /// </summary>
        Task<Dictionary<LogLevel, int>> GetSystemLogStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null);

        #endregion

        #region 操作日志

        /// <summary>
        /// 记录操作日志
        /// </summary>
        /// <param name="action">操作动作</param>
        /// <param name="resource">操作资源</param>
        /// <param name="description">操作描述</param>
        /// <param name="userId">用户ID</param>
        /// <param name="resourceId">资源ID</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="userAgent">用户代理</param>
        /// <param name="result">操作结果</param>
        Task LogOperationAsync(string action, string resource, string description, 
            int? userId = null, int? resourceId = null, string ipAddress = null, 
            string userAgent = null, string result = null);

        /// <summary>
        /// 记录成功操作
        /// </summary>
        Task LogOperationSuccessAsync(string action, string resource, string description, 
            int? userId = null, int? resourceId = null);

        /// <summary>
        /// 记录失败操作
        /// </summary>
        Task LogOperationFailureAsync(string action, string resource, string description, 
            string errorMessage, int? userId = null, int? resourceId = null);

        /// <summary>
        /// 获取操作日志分页列表
        /// </summary>
        Task<(List<OperationLog> items, int totalCount)> GetOperationLogsPagedAsync(
            int page, int pageSize,
            int? userId = null,
            string action = null,
            string resource = null,
            DateTime? startTime = null,
            DateTime? endTime = null);

        /// <summary>
        /// 获取操作统计
        /// </summary>
        Task<Dictionary<string, int>> GetOperationStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null);

        #endregion

        #region 通信日志

        /// <summary>
        /// 记录通信日志
        /// </summary>
        /// <param name="direction">通信方向</param>
        /// <param name="protocol">通信协议</param>
        /// <param name="data">通信数据</param>
        /// <param name="channelId">通道ID</param>
        /// <param name="deviceId">设备ID</param>
        /// <param name="status">通信状态</param>
        /// <param name="responseTime">响应时间</param>
        /// <param name="errorMessage">错误消息</param>
        Task LogCommunicationAsync(string direction, string protocol, string data, 
            int? channelId = null, int? deviceId = null, string status = null, 
            int? responseTime = null, string errorMessage = null);

        /// <summary>
        /// 记录发送日志
        /// </summary>
        Task LogSendAsync(string protocol, string data, int? channelId = null, int? deviceId = null);

        /// <summary>
        /// 记录接收日志
        /// </summary>
        Task LogReceiveAsync(string protocol, string data, int? channelId = null, int? deviceId = null);

        /// <summary>
        /// 记录通信成功
        /// </summary>
        Task LogCommunicationSuccessAsync(string direction, string protocol, string data, 
            int responseTime, int? channelId = null, int? deviceId = null);

        /// <summary>
        /// 记录通信失败
        /// </summary>
        Task LogCommunicationFailureAsync(string direction, string protocol, string data, 
            string errorMessage, int responseTime = 0, int? channelId = null, int? deviceId = null);

        /// <summary>
        /// 获取通信日志分页列表
        /// </summary>
        Task<(List<CommunicationLog> items, int totalCount)> GetCommunicationLogsPagedAsync(
            int page, int pageSize,
            int? channelId = null,
            int? deviceId = null,
            string direction = null,
            string protocol = null,
            string status = null,
            DateTime? startTime = null,
            DateTime? endTime = null);

        /// <summary>
        /// 获取通信统计
        /// </summary>
        Task<Dictionary<string, int>> GetCommunicationStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// 获取平均响应时间
        /// </summary>
        Task<double> GetAverageResponseTimeAsync(DateTime? startTime = null, DateTime? endTime = null);

        #endregion

        #region 日志管理

        /// <summary>
        /// 清理过期日志
        /// </summary>
        /// <param name="beforeTime">清理时间点</param>
        Task<int> CleanupLogsAsync(DateTime beforeTime);

        /// <summary>
        /// 清理系统日志
        /// </summary>
        Task<int> CleanupSystemLogsAsync(DateTime beforeTime);

        /// <summary>
        /// 清理操作日志
        /// </summary>
        Task<int> CleanupOperationLogsAsync(DateTime beforeTime);

        /// <summary>
        /// 清理通信日志
        /// </summary>
        Task<int> CleanupCommunicationLogsAsync(DateTime beforeTime);

        /// <summary>
        /// 导出日志
        /// </summary>
        Task<byte[]> ExportLogsAsync(DateTime startTime, DateTime endTime, LogCategory? category = null);

        #endregion
    }
} 