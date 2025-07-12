using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Enums;

// 使用别名解决 LogLevel 命名空间冲突
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;
using DomainLogLevel = Wombat.CommGateway.Domain.Enums.LogLevel;
using Wombat.Extensions.AutoGenerator.Attributes;

namespace Wombat.CommGateway.Application.Common.Logging
{
    /// <summary>
    /// 应用程序日志记录器装饰器实现
    /// 包装标准 ILogger 功能，添加数据库日志记录能力
    /// </summary>
    /// <typeparam name="T">日志类型</typeparam>
    /// 
    public class ApplicationLogger<T> : IApplicationLogger<T>
    {
        private readonly ILogger<T> _logger;
        private readonly ILogService _logService;
        private readonly DatabaseLoggerOptions _options;
        private readonly Type _serviceType;

        public ApplicationLogger(
            ILogger<T> logger,
            ILogService logService,
            IOptions<DatabaseLoggerOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serviceType = typeof(T);
        }

        #region ILogger<T> 装饰器实现

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(MsLogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(MsLogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // 记录到原始日志
            _logger.Log(logLevel, eventId, state, exception, formatter);

            // 如果启用数据库日志记录且级别符合条件
            if (_options.EnableDatabaseLogging && _options.DatabaseLogLevels.Contains(logLevel))
            {
                var message = formatter(state, exception);
                _ = Task.Run(async () => await RecordToDatabaseAsync(logLevel, message, exception));
            }
        }

        #endregion

        #region IApplicationLogger<T> 扩展方法

        /// <summary>
        /// 记录操作日志到数据库
        /// </summary>
        public async Task LogOperationAsync(string message, string action, string resource,
            Exception exception = null, int? userId = null, int? resourceId = null)
        {
            try
            {
                // 记录到原始日志
                if (exception != null)
                    _logger.LogError(exception, "操作失败: {Message}", message);
                else
                    _logger.LogInformation("操作: {Message}", message);

                // 记录到数据库
                await _logService.LogOperationAsync(action, resource, message, userId, resourceId, 
                    result: exception == null ? "Success" : $"Failed: {exception.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录操作日志失败");
            }
        }

        /// <summary>
        /// 记录通信日志到数据库
        /// </summary>
        public async Task LogCommunicationAsync(string message, string direction, string protocol,
            string data, Exception exception = null, int? channelId = null, int? deviceId = null, int? responseTime = null)
        {
            try
            {
                // 记录到原始日志
                if (exception != null)
                    _logger.LogError(exception, "通信失败: {Message}", message);
                else
                    _logger.LogInformation("通信: {Message}", message);

                // 记录到数据库
                var errorMessage = exception?.Message;
                await _logService.LogCommunicationAsync(direction, protocol, data, channelId, deviceId, 
                    exception == null ? "Success" : "Failed", responseTime, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录通信日志失败");
            }
        }

        /// <summary>
        /// 记录系统日志到数据库
        /// </summary>
        public async Task LogSystemAsync(string message, LogCategory category = LogCategory.System,
            Exception exception = null, int? userId = null, Dictionary<string, object> properties = null)
        {
            try
            {
                // 记录到原始日志
                if (exception != null)
                    _logger.LogError(exception, "系统错误: {Message}", message);
                else
                    _logger.LogInformation("系统: {Message}", message);

                // 记录到数据库
                var level = exception != null ? DomainLogLevel.Error : DomainLogLevel.Information;
                await _logService.LogSystemAsync(level, message, category, GetSourceName(), userId, exception, properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录系统日志失败");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 记录到数据库
        /// </summary>
        private async Task RecordToDatabaseAsync(MsLogLevel logLevel, string message, Exception exception)
        {
            try
            {
                var category = _options.GetLogCategory(_serviceType);
                var domainLogLevel = ConvertLogLevel(logLevel);
                var source = GetSourceName();

                // 根据消息内容和服务类型智能判断日志类型
                if (_options.IsCommunicationLog(message))
                {
                    // 通信日志
                    await _logService.LogCommunicationAsync("Unknown", "Unknown", message, 
                        status: exception == null ? "Success" : "Failed", 
                        errorMessage: exception?.Message);
                }
                else if (_options.IsOperationLog(message))
                {
                    // 操作日志
                    await _logService.LogOperationAsync("Unknown", source, message, 
                        result: exception == null ? "Success" : $"Failed: {exception.Message}");
                }
                else
                {
                    // 系统日志
                    await _logService.LogSystemAsync(domainLogLevel, message, category, source, 
                        exception: exception);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录数据库日志失败");
            }
        }

        /// <summary>
        /// 转换日志级别
        /// </summary>
        private DomainLogLevel ConvertLogLevel(MsLogLevel logLevel)
        {
            return logLevel switch
            {
                MsLogLevel.Trace => DomainLogLevel.Trace,
                MsLogLevel.Debug => DomainLogLevel.Debug,
                MsLogLevel.Information => DomainLogLevel.Information,
                MsLogLevel.Warning => DomainLogLevel.Warning,
                MsLogLevel.Error => DomainLogLevel.Error,
                MsLogLevel.Critical => DomainLogLevel.Critical,
                _ => DomainLogLevel.Information
            };
        }

        /// <summary>
        /// 获取源名称
        /// </summary>
        private string GetSourceName()
        {
            return _serviceType.Name;
        }

        #endregion
    }
} 