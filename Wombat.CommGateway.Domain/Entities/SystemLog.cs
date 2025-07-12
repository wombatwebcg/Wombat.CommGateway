using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Common;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 系统日志实体
    /// </summary>
    [Table(Name = "SystemLogs")]
    public class SystemLog : AggregateRoot
    {
        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; set; }
        
        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 消息模板
        /// </summary>
        public string Template { get; set; }
        
        /// <summary>
        /// 日志时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 异常信息
        /// </summary>
        public string Exception { get; set; }
        
        /// <summary>
        /// 日志属性（JSON格式）
        /// </summary>
        [JsonMap]
        public Dictionary<string, object> Properties { get; set; }
        
        /// <summary>
        /// 用户ID
        /// </summary>
        public int? UserId { get; set; }
        
        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionId { get; set; }
        
        /// <summary>
        /// 请求ID
        /// </summary>
        public string RequestId { get; set; }
        
        /// <summary>
        /// 日志来源
        /// </summary>
        public string Source { get; set; }
        
        /// <summary>
        /// 日志分类
        /// </summary>
        public LogCategory Category { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        
        /// <summary>
        /// 私有构造函数
        /// </summary>
        private SystemLog() { }
        
        /// <summary>
        /// 创建系统日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="category">日志分类</param>
        /// <param name="source">日志来源</param>
        public SystemLog(LogLevel level, string message, LogCategory category, string source = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("日志消息不能为空", nameof(message));
            
            Level = level;
            Message = message;
            Category = category;
            Source = source ?? "Unknown";
            Timestamp = DateTime.Now;
            Properties = new Dictionary<string, object>();
            CreateTime = DateTime.Now;
        }
        
        /// <summary>
        /// 设置异常信息
        /// </summary>
        /// <param name="exception">异常对象</param>
        public void SetException(Exception exception)
        {
            if (exception != null)
            {
                Exception = exception.ToString();
            }
        }
        
        /// <summary>
        /// 设置消息模板
        /// </summary>
        /// <param name="template">消息模板</param>
        public void SetTemplate(string template)
        {
            Template = template;
        }
        
        /// <summary>
        /// 设置用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="sessionId">会话ID</param>
        public void SetUserInfo(int? userId, string sessionId)
        {
            UserId = userId;
            SessionId = sessionId;
        }
        
        /// <summary>
        /// 设置请求信息
        /// </summary>
        /// <param name="requestId">请求ID</param>
        public void SetRequestInfo(string requestId)
        {
            RequestId = requestId;
        }
        
        /// <summary>
        /// 添加属性
        /// </summary>
        /// <param name="key">属性键</param>
        /// <param name="value">属性值</param>
        public void AddProperty(string key, object value)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                Properties[key] = value;
            }
        }
        
        /// <summary>
        /// 批量添加属性
        /// </summary>
        /// <param name="properties">属性字典</param>
        public void AddProperties(Dictionary<string, object> properties)
        {
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    Properties[property.Key] = property.Value;
                }
            }
        }
    }
} 