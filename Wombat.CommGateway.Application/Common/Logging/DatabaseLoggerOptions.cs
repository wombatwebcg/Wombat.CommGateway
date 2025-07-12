using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Enums;
using System.Linq; // Added for .Any()

// 使用别名解决 LogLevel 命名空间冲突
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Wombat.CommGateway.Application.Common.Logging
{
    /// <summary>
    /// 数据库日志配置选项
    /// </summary>
    public class DatabaseLoggerOptions
    {
        /// <summary>
        /// 是否启用数据库日志记录
        /// </summary>
        public bool EnableDatabaseLogging { get; set; } = true;

        /// <summary>
        /// 需要记录到数据库的日志级别
        /// </summary>
        public HashSet<MsLogLevel> DatabaseLogLevels { get; set; } = new HashSet<MsLogLevel>
        {
            MsLogLevel.Information,
            MsLogLevel.Warning,
            MsLogLevel.Error,
            MsLogLevel.Critical
        };

        /// <summary>
        /// 服务类型到日志类别的映射
        /// </summary>
        public Dictionary<string, LogCategory> ServiceLogCategoryMapping { get; set; } = new Dictionary<string, LogCategory>
        {
            // 数据采集相关服务
            { "DataCollectionService", LogCategory.DataCollection },
            { "DataCollectionHubService", LogCategory.DataCollection },
            { "TimeWheelScheduler", LogCategory.DataCollection },
            { "CacheManager", LogCategory.DataCollection },
            { "ConnectionPoolManager", LogCategory.DataCollection },
            { "SubscriptionManager", LogCategory.DataCollection },
            
            // 通信相关服务
            { "WebSocketService", LogCategory.Communication },
            { "DataDistributionService", LogCategory.Communication },
            { "ChannelService", LogCategory.Communication },
            
            // 设备相关服务
            { "DeviceService", LogCategory.System },
            { "DevicePointService", LogCategory.System },
            { "DeviceGroupService", LogCategory.System },
            
            // 规则引擎相关服务
            { "RuleService", LogCategory.System },
            { "RuleEngineService", LogCategory.System },
            
            // 协议相关服务
            { "ProtocolConfigService", LogCategory.Communication },
            
            // 系统相关服务
            { "LogService", LogCategory.System },
            { "AuthenticationService", LogCategory.Security },
            { "AuthorizationService", LogCategory.Security }
        };

        /// <summary>
        /// 通信日志关键字映射
        /// 用于判断日志消息是否为通信日志
        /// </summary>
        public HashSet<string> CommunicationKeywords { get; set; } = new HashSet<string>
        {
            "连接", "断开", "发送", "接收", "通信", "协议", "数据", "响应", "超时", "重连",
            "connection", "disconnect", "send", "receive", "communication", "protocol", "data", "response", "timeout", "reconnect"
        };

        /// <summary>
        /// 操作日志关键字映射
        /// 用于判断日志消息是否为操作日志
        /// </summary>
        public HashSet<string> OperationKeywords { get; set; } = new HashSet<string>
        {
            "创建", "更新", "删除", "启用", "禁用", "执行", "处理", "配置", "设置", "添加", "移除",
            "create", "update", "delete", "enable", "disable", "execute", "process", "configure", "set", "add", "remove"
        };

        /// <summary>
        /// 获取服务对应的日志类别
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns>日志类别</returns>
        public LogCategory GetLogCategory(Type serviceType)
        {
            if (serviceType == null)
                return LogCategory.System;

            var serviceName = serviceType.Name;
            
            // 移除泛型标记
            if (serviceName.Contains("`"))
            {
                serviceName = serviceName.Substring(0, serviceName.IndexOf("`"));
            }

            return ServiceLogCategoryMapping.TryGetValue(serviceName, out var category) 
                ? category 
                : LogCategory.System;
        }

        /// <summary>
        /// 判断消息是否为通信日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>是否为通信日志</returns>
        public bool IsCommunicationLog(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            var lowerMessage = message.ToLower();
            return CommunicationKeywords.Any(keyword => lowerMessage.Contains(keyword.ToLower()));
        }

        /// <summary>
        /// 判断消息是否为操作日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>是否为操作日志</returns>
        public bool IsOperationLog(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            var lowerMessage = message.ToLower();
            return OperationKeywords.Any(keyword => lowerMessage.Contains(keyword.ToLower()));
        }
    }
} 