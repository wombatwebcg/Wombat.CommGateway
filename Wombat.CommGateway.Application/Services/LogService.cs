using Wombat.CommGateway.Application.Common.Logging;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;
using Wombat.CommGateway.Domain.Repositories;
using Wombat.Extensions.AutoGenerator.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// 使用别名解决 LogLevel 命名空间冲突
using DomainLogLevel = Wombat.CommGateway.Domain.Enums.LogLevel;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Wombat.CommGateway.Application.Services;

/// <summary>
/// 日志服务实现
/// </summary>
[AutoInject<ILogService>(ServiceLifetime = ServiceLifetime.Scoped)]
public class LogService : ILogService
{
    private readonly ISystemLogRepository _systemLogRepository;
    private readonly IOperationLogRepository _operationLogRepository;
    private readonly ICommunicationLogRepository _communicationLogRepository;
    private readonly ILogEventBus _logEventBus;
    private readonly ILogger<LogService> _logger;

    public LogService(
        ISystemLogRepository systemLogRepository,
        IOperationLogRepository operationLogRepository,
        ICommunicationLogRepository communicationLogRepository,
        ILogEventBus logEventBus,
        ILogger<LogService> logger)
    {
        _systemLogRepository = systemLogRepository;
        _operationLogRepository = operationLogRepository;
        _communicationLogRepository = communicationLogRepository;
        _logEventBus = logEventBus;
        _logger = logger;
    }

    #region 系统日志

    /// <summary>
    /// 记录系统日志
    /// </summary>
    public async Task LogSystemAsync(DomainLogLevel level, string message, LogCategory category, 
        string source = null, int? userId = null, Exception exception = null, Dictionary<string, object> properties = null)
    {
        try
        {
            var systemLog = new SystemLog(level, message, category, source);
            if (exception != null)
            {
                systemLog.SetException(exception);
            }
            if (userId.HasValue)
            {
                systemLog.SetUserInfo(userId, null);
            }
            if (properties != null)
            {
                systemLog.AddProperties(properties);
            }

            await _systemLogRepository.InsertAsync(systemLog);
            
            // 发布系统日志事件
            await _logEventBus.PublishAsync(LogEvent.CreateSystemLogEvent(systemLog));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录系统日志失败: {Message}", message);
        }
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    public async Task LogInformationAsync(string message, LogCategory category = LogCategory.System, string source = null)
    {
        await LogSystemAsync(DomainLogLevel.Information, message, category, source);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public async Task LogWarningAsync(string message, LogCategory category = LogCategory.System, string source = null)
    {
        await LogSystemAsync(DomainLogLevel.Warning, message, category, source);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public async Task LogErrorAsync(string message, Exception exception = null, LogCategory category = LogCategory.System, string source = null)
    {
        await LogSystemAsync(DomainLogLevel.Error, message, category, source, null, exception);
    }

    /// <summary>
    /// 记录调试日志
    /// </summary>
    public async Task LogDebugAsync(string message, LogCategory category = LogCategory.System, string source = null)
    {
        await LogSystemAsync(DomainLogLevel.Debug, message, category, source);
    }

    /// <summary>
    /// 获取系统日志分页列表
    /// </summary>
    public async Task<(List<SystemLog> items, int totalCount)> GetSystemLogsPagedAsync(
        int page, int pageSize, 
        DomainLogLevel? level = null, 
        LogCategory? category = null, 
        DateTime? startTime = null, 
        DateTime? endTime = null,
        string keyword = null)
    {
        try
        {
            return await _systemLogRepository.GetPagedAsync(page, pageSize, level, category, startTime, endTime, keyword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统日志分页列表失败");
            return (new List<SystemLog>(), 0);
        }
    }

    /// <summary>
    /// 获取系统日志统计
    /// </summary>
    public async Task<Dictionary<DomainLogLevel, int>> GetSystemLogStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null)
    {
        try
        {
            return await _systemLogRepository.GetStatisticsAsync(startTime, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统日志统计失败");
            return new Dictionary<DomainLogLevel, int>();
        }
    }

    #endregion

    #region 操作日志

    /// <summary>
    /// 记录操作日志
    /// </summary>
    public async Task LogOperationAsync(string action, string resource, string description, 
        int? userId = null, int? resourceId = null, string ipAddress = null, 
        string userAgent = null, string result = null)
    {
        try
        {
            var operationLog = new OperationLog(action, resource, description, userId);
            if (resourceId.HasValue)
            {
                operationLog.SetResourceId(resourceId.Value);
            }
            if (!string.IsNullOrEmpty(ipAddress) || !string.IsNullOrEmpty(userAgent))
            {
                operationLog.SetNetworkInfo(ipAddress, userAgent);
            }
            if (!string.IsNullOrEmpty(result))
            {
                operationLog.SetResult(result);
            }

            await _operationLogRepository.InsertAsync(operationLog);
            
            // 发布操作日志事件
            await _logEventBus.PublishAsync(LogEvent.CreateOperationLogEvent(operationLog));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录操作日志失败: {Action}", action);
        }
    }

    /// <summary>
    /// 记录成功操作
    /// </summary>
    public async Task LogOperationSuccessAsync(string action, string resource, string description, 
        int? userId = null, int? resourceId = null)
    {
        await LogOperationAsync(action, resource, description, userId, resourceId, result: "Success");
    }

    /// <summary>
    /// 记录失败操作
    /// </summary>
    public async Task LogOperationFailureAsync(string action, string resource, string description, 
        string errorMessage, int? userId = null, int? resourceId = null)
    {
        await LogOperationAsync(action, resource, description, userId, resourceId, result: $"Failed: {errorMessage}");
    }

    /// <summary>
    /// 获取操作日志分页列表
    /// </summary>
    public async Task<(List<OperationLog> items, int totalCount)> GetOperationLogsPagedAsync(
        int page, int pageSize,
        int? userId = null,
        string action = null,
        string resource = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        try
        {
            return await _operationLogRepository.GetPagedAsync(page, pageSize, userId, action, resource, startTime, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作日志分页列表失败");
            return (new List<OperationLog>(), 0);
        }
    }

    /// <summary>
    /// 获取操作统计
    /// </summary>
    public async Task<Dictionary<string, int>> GetOperationStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null)
    {
        try
        {
            return await _operationLogRepository.GetActionStatisticsAsync(startTime, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作统计失败");
            return new Dictionary<string, int>();
        }
    }

    #endregion

    #region 通信日志

    /// <summary>
    /// 记录通信日志
    /// </summary>
    public async Task LogCommunicationAsync(string direction, string protocol, string data, 
        int? channelId = null, int? deviceId = null, string status = null, 
        int? responseTime = null, string errorMessage = null)
    {
        try
        {
            var communicationLog = new CommunicationLog(direction, protocol, data, channelId, deviceId);
            if (responseTime.HasValue)
            {
                if (string.IsNullOrEmpty(errorMessage))
                {
                    communicationLog.SetSuccess(responseTime.Value);
                }
                else
                {
                    communicationLog.SetFailure(errorMessage, responseTime.Value);
                }
            }
            else if (!string.IsNullOrEmpty(errorMessage))
            {
                communicationLog.SetFailure(errorMessage);
            }
            else if (!string.IsNullOrEmpty(status))
            {
                if (status == "Timeout")
                {
                    communicationLog.SetTimeout(responseTime ?? 0);
                }
            }

            await _communicationLogRepository.InsertAsync(communicationLog);
            
            // 发布通信日志事件
            await _logEventBus.PublishAsync(LogEvent.CreateCommunicationLogEvent(communicationLog));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录通信日志失败: {Direction}-{Protocol}", direction, protocol);
        }
    }

    /// <summary>
    /// 记录发送日志
    /// </summary>
    public async Task LogSendAsync(string protocol, string data, int? channelId = null, int? deviceId = null)
    {
        await LogCommunicationAsync("Send", protocol, data, channelId, deviceId);
    }

    /// <summary>
    /// 记录接收日志
    /// </summary>
    public async Task LogReceiveAsync(string protocol, string data, int? channelId = null, int? deviceId = null)
    {
        await LogCommunicationAsync("Receive", protocol, data, channelId, deviceId);
    }

    /// <summary>
    /// 记录通信成功
    /// </summary>
    public async Task LogCommunicationSuccessAsync(string direction, string protocol, string data, 
        int responseTime, int? channelId = null, int? deviceId = null)
    {
        await LogCommunicationAsync(direction, protocol, data, channelId, deviceId, "Success", responseTime);
    }

    /// <summary>
    /// 记录通信失败
    /// </summary>
    public async Task LogCommunicationFailureAsync(string direction, string protocol, string data, 
        string errorMessage, int responseTime = 0, int? channelId = null, int? deviceId = null)
    {
        await LogCommunicationAsync(direction, protocol, data, channelId, deviceId, "Failed", responseTime, errorMessage);
    }

    /// <summary>
    /// 获取通信日志分页列表
    /// </summary>
    public async Task<(List<CommunicationLog> items, int totalCount)> GetCommunicationLogsPagedAsync(
        int page, int pageSize,
        int? channelId = null,
        int? deviceId = null,
        string direction = null,
        string protocol = null,
        string status = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        try
        {
            return await _communicationLogRepository.GetPagedAsync(page, pageSize, channelId, deviceId, direction, protocol, status, startTime, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取通信日志分页列表失败");
            return (new List<CommunicationLog>(), 0);
        }
    }

    /// <summary>
    /// 获取通信统计
    /// </summary>
    public async Task<Dictionary<string, int>> GetCommunicationStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null)
    {
        try
        {
            return await _communicationLogRepository.GetStatusStatisticsAsync(startTime, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取通信统计失败");
            return new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// 获取平均响应时间
    /// </summary>
    public async Task<double> GetAverageResponseTimeAsync(DateTime? startTime = null, DateTime? endTime = null)
    {
        try
        {
            return await _communicationLogRepository.GetAverageResponseTimeAsync(startTime, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取平均响应时间失败");
            return 0;
        }
    }

    #endregion

    #region 日志管理

    /// <summary>
    /// 清理过期日志
    /// </summary>
    public async Task<int> CleanupLogsAsync(DateTime beforeTime)
    {
        try
        {
            int totalDeleted = 0;
            
            // 清理系统日志
            int systemDeleted = await _systemLogRepository.DeleteBeforeAsync(beforeTime);
            totalDeleted += systemDeleted;
            
            // 清理操作日志
            int operationDeleted = await _operationLogRepository.DeleteBeforeAsync(beforeTime);
            totalDeleted += operationDeleted;
            
            // 清理通信日志
            int communicationDeleted = await _communicationLogRepository.DeleteBeforeAsync(beforeTime);
            totalDeleted += communicationDeleted;
            
            _logger.LogInformation("清理过期日志完成，删除记录数: 系统日志={SystemDeleted}, 操作日志={OperationDeleted}, 通信日志={CommunicationDeleted}",
                systemDeleted, operationDeleted, communicationDeleted);
            
            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期日志失败");
            return 0;
        }
    }

    /// <summary>
    /// 清理系统日志
    /// </summary>
    public async Task<int> CleanupSystemLogsAsync(DateTime beforeTime)
    {
        try
        {
            int deleted = await _systemLogRepository.DeleteBeforeAsync(beforeTime);
            _logger.LogInformation("清理系统日志完成，删除记录数={Deleted}", deleted);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理系统日志失败");
            return 0;
        }
    }

    /// <summary>
    /// 清理操作日志
    /// </summary>
    public async Task<int> CleanupOperationLogsAsync(DateTime beforeTime)
    {
        try
        {
            int deleted = await _operationLogRepository.DeleteBeforeAsync(beforeTime);
            _logger.LogInformation("清理操作日志完成，删除记录数={Deleted}", deleted);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理操作日志失败");
            return 0;
        }
    }

    /// <summary>
    /// 清理通信日志
    /// </summary>
    public async Task<int> CleanupCommunicationLogsAsync(DateTime beforeTime)
    {
        try
        {
            int deleted = await _communicationLogRepository.DeleteBeforeAsync(beforeTime);
            _logger.LogInformation("清理通信日志完成，删除记录数={Deleted}", deleted);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理通信日志失败");
            return 0;
        }
    }

    /// <summary>
    /// 导出日志
    /// </summary>
    public async Task<byte[]> ExportLogsAsync(DateTime startTime, DateTime endTime, LogCategory? category = null)
    {
        try
        {
            var systemLogs = await _systemLogRepository.GetByTimeRangeAsync(startTime, endTime);
            var operationLogs = await _operationLogRepository.GetPagedAsync(1, int.MaxValue, null, null, null, startTime, endTime);
            var communicationLogs = await _communicationLogRepository.GetPagedAsync(1, int.MaxValue, null, null, null, null, null, startTime, endTime);

            // 创建简单的CSV格式导出
            var csvBuilder = new System.Text.StringBuilder();
            csvBuilder.AppendLine("时间,类型,级别,分类,消息,来源");
            
            foreach (var log in systemLogs)
            {
                if (category == null || log.Category == category)
                {
                    csvBuilder.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},系统日志,{log.Level},{log.Category},{log.Message},{log.Source}");
                }
            }
            
            foreach (var log in operationLogs.items)
            {
                csvBuilder.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},操作日志,Info,Operation,{log.Description},{log.Action}");
            }
            
            foreach (var log in communicationLogs.items)
            {
                csvBuilder.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss},通信日志,Info,Communication,{log.Direction},{log.Protocol}");
            }

            return System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出日志失败");
            return Array.Empty<byte>();
        }
    }

    #endregion
} 