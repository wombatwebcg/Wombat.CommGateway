using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Common.Logging
{
    /// <summary>
    /// 应用程序日志记录器接口
    /// 扩展了标准 ILogger 功能，添加了数据库日志记录能力
    /// </summary>
    /// <typeparam name="T">日志类型</typeparam>
    public interface IApplicationLogger<T> : ILogger<T>
    {
        /// <summary>
        /// 记录操作日志到数据库
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="action">操作动作</param>
        /// <param name="resource">操作资源</param>
        /// <param name="exception">异常信息（可选）</param>
        /// <param name="userId">用户ID（可选）</param>
        /// <param name="resourceId">资源ID（可选）</param>
        /// <returns></returns>
        Task LogOperationAsync(string message, string action, string resource, 
            Exception exception = null, int? userId = null, int? resourceId = null);

        /// <summary>
        /// 记录通信日志到数据库
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="direction">通信方向</param>
        /// <param name="protocol">通信协议</param>
        /// <param name="data">通信数据</param>
        /// <param name="exception">异常信息（可选）</param>
        /// <param name="channelId">通道ID（可选）</param>
        /// <param name="deviceId">设备ID（可选）</param>
        /// <param name="responseTime">响应时间（可选）</param>
        /// <returns></returns>
        Task LogCommunicationAsync(string message, string direction, string protocol, 
            string data, Exception exception = null, int? channelId = null, 
            int? deviceId = null, int? responseTime = null);

        /// <summary>
        /// 记录系统日志到数据库
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">日志类别</param>
        /// <param name="exception">异常信息（可选）</param>
        /// <param name="userId">用户ID（可选）</param>
        /// <param name="properties">附加属性（可选）</param>
        /// <returns></returns>
        Task LogSystemAsync(string message, LogCategory category = LogCategory.System, 
            Exception exception = null, int? userId = null, 
            System.Collections.Generic.Dictionary<string, object> properties = null);
    }
} 