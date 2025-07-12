using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Common.Logging
{
    /// <summary>
    /// 日志事件类型
    /// </summary>
    public enum LogEventType
    {
        SystemLog,
        OperationLog,
        CommunicationLog
    }

    /// <summary>
    /// 日志事件数据
    /// </summary>
    public class LogEvent
    {
        /// <summary>
        /// 事件类型
        /// </summary>
        public LogEventType EventType { get; set; }

        /// <summary>
        /// 日志数据
        /// </summary>
        public object LogData { get; set; }

        /// <summary>
        /// 事件时间
        /// </summary>
        public DateTime EventTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 事件ID
        /// </summary>
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 创建系统日志事件
        /// </summary>
        public static LogEvent CreateSystemLogEvent(SystemLog log)
        {
            return new LogEvent
            {
                EventType = LogEventType.SystemLog,
                LogData = log
            };
        }

        /// <summary>
        /// 创建操作日志事件
        /// </summary>
        public static LogEvent CreateOperationLogEvent(OperationLog log)
        {
            return new LogEvent
            {
                EventType = LogEventType.OperationLog,
                LogData = log
            };
        }

        /// <summary>
        /// 创建通信日志事件
        /// </summary>
        public static LogEvent CreateCommunicationLogEvent(CommunicationLog log)
        {
            return new LogEvent
            {
                EventType = LogEventType.CommunicationLog,
                LogData = log
            };
        }
    }

    /// <summary>
    /// 日志事件处理器接口
    /// </summary>
    public interface ILogEventHandler
    {
        /// <summary>
        /// 处理日志事件
        /// </summary>
        Task HandleAsync(LogEvent logEvent);

        /// <summary>
        /// 获取处理器名称
        /// </summary>
        string HandlerName { get; }

        /// <summary>
        /// 是否启用
        /// </summary>
        bool IsEnabled { get; }
    }

    /// <summary>
    /// 日志事件总线接口
    /// </summary>
    public interface ILogEventBus : IAsyncDisposable
    {
        /// <summary>
        /// 发布日志事件
        /// </summary>
        Task PublishAsync(LogEvent logEvent);

        /// <summary>
        /// 订阅日志事件
        /// </summary>
        IAsyncDisposable Subscribe(ILogEventHandler handler);

        /// <summary>
        /// 订阅日志事件（函数式）
        /// </summary>
        IAsyncDisposable Subscribe(Func<LogEvent, Task> handler, string handlerName = null);

        /// <summary>
        /// 获取订阅者数量
        /// </summary>
        int GetSubscriberCount();

        /// <summary>
        /// 获取事件统计
        /// </summary>
        Task<Dictionary<LogEventType, long>> GetEventStatisticsAsync();
    }

    /// <summary>
    /// 日志事件总线实现
    /// </summary>
    [AutoInject<ILogEventBus>(ServiceLifetime = ServiceLifetime.Singleton)]
    public class LogEventBus : ILogEventBus
    {
        private readonly ConcurrentDictionary<string, ILogEventHandler> _handlers = new();
        private readonly ConcurrentDictionary<LogEventType, long> _eventCounters = new();
        private readonly ILogger<LogEventBus> _logger;
        private volatile bool _disposed = false;

        public LogEventBus(ILogger<LogEventBus> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // 初始化事件计数器
            foreach (LogEventType eventType in Enum.GetValues<LogEventType>())
            {
                _eventCounters[eventType] = 0;
            }

            _logger.LogInformation("日志事件总线已初始化");
        }

        /// <inheritdoc/>
        public async Task PublishAsync(LogEvent logEvent)
        {
            if (_disposed)
                return;

            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));

            try
            {
                // 增加事件计数
                _eventCounters.AddOrUpdate(logEvent.EventType, 1, (key, value) => value + 1);

                // 获取所有启用的处理器
                var enabledHandlers = _handlers.Values.Where(h => h.IsEnabled).ToList();

                if (enabledHandlers.Count == 0)
                {
                    _logger.LogDebug("没有找到启用的日志事件处理器，事件类型: {EventType}", logEvent.EventType);
                    return;
                }

                // 并行处理事件
                var tasks = enabledHandlers.Select(async handler =>
                {
                    try
                    {
                        await handler.HandleAsync(logEvent);
                        _logger.LogTrace("日志事件处理成功，处理器: {HandlerName}, 事件ID: {EventId}", 
                            handler.HandlerName, logEvent.EventId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "日志事件处理失败，处理器: {HandlerName}, 事件ID: {EventId}", 
                            handler.HandlerName, logEvent.EventId);
                    }
                });

                await Task.WhenAll(tasks);

                _logger.LogTrace("日志事件发布完成，事件类型: {EventType}, 事件ID: {EventId}, 处理器数量: {HandlerCount}", 
                    logEvent.EventType, logEvent.EventId, enabledHandlers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布日志事件时发生错误，事件类型: {EventType}, 事件ID: {EventId}", 
                    logEvent.EventType, logEvent.EventId);
            }
        }

        /// <inheritdoc/>
        public IAsyncDisposable Subscribe(ILogEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var handlerId = $"{handler.HandlerName}_{Guid.NewGuid():N}";
            _handlers[handlerId] = handler;

            _logger.LogInformation("日志事件处理器已订阅，处理器: {HandlerName}, ID: {HandlerId}", 
                handler.HandlerName, handlerId);

            return new LogEventSubscription(() =>
            {
                _handlers.TryRemove(handlerId, out _);
                _logger.LogInformation("日志事件处理器已取消订阅，处理器: {HandlerName}, ID: {HandlerId}", 
                    handler.HandlerName, handlerId);
            });
        }

        /// <inheritdoc/>
        public IAsyncDisposable Subscribe(Func<LogEvent, Task> handler, string handlerName = null)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var functionHandler = new FunctionLogEventHandler(handler, handlerName ?? "FunctionHandler");
            return Subscribe(functionHandler);
        }

        /// <inheritdoc/>
        public int GetSubscriberCount()
        {
            return _handlers.Count;
        }

        /// <inheritdoc/>
        public Task<Dictionary<LogEventType, long>> GetEventStatisticsAsync()
        {
            return Task.FromResult(_eventCounters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            if (_disposed)
                return ValueTask.CompletedTask;

            _disposed = true;
            _handlers.Clear();
            _eventCounters.Clear();

            _logger.LogInformation("日志事件总线已释放");
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 函数式日志事件处理器
        /// </summary>
        private class FunctionLogEventHandler : ILogEventHandler
        {
            private readonly Func<LogEvent, Task> _handler;

            public FunctionLogEventHandler(Func<LogEvent, Task> handler, string handlerName)
            {
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
                HandlerName = handlerName ?? "Unknown";
            }

            public string HandlerName { get; }

            public bool IsEnabled => true;

            public Task HandleAsync(LogEvent logEvent)
            {
                return _handler(logEvent);
            }
        }

        /// <summary>
        /// 日志事件订阅
        /// </summary>
        private class LogEventSubscription : IAsyncDisposable
        {
            private readonly Action _unsubscribe;
            private volatile bool _disposed = false;

            public LogEventSubscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
            }

            public ValueTask DisposeAsync()
            {
                if (_disposed)
                    return ValueTask.CompletedTask;

                _disposed = true;
                _unsubscribe();
                return ValueTask.CompletedTask;
            }
        }
    }
} 