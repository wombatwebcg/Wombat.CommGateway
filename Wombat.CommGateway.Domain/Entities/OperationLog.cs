using FreeSql.DataAnnotations;
using System;
using Wombat.CommGateway.Domain.Common;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 操作日志实体
    /// </summary>
    [Table(Name = "OperationLogs")]
    public class OperationLog : AggregateRoot
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public int? UserId { get; set; }
        
        /// <summary>
        /// 操作动作
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// 操作资源
        /// </summary>
        public string Resource { get; set; }
        
        /// <summary>
        /// 资源ID
        /// </summary>
        public int? ResourceId { get; set; }
        
        /// <summary>
        /// 操作描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; }
        
        /// <summary>
        /// 用户代理
        /// </summary>
        public string UserAgent { get; set; }
        
        /// <summary>
        /// 操作时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 操作结果
        /// </summary>
        public string Result { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        
        /// <summary>
        /// 私有构造函数
        /// </summary>
        private OperationLog() { }
        
        /// <summary>
        /// 创建操作日志
        /// </summary>
        /// <param name="action">操作动作</param>
        /// <param name="resource">操作资源</param>
        /// <param name="description">操作描述</param>
        /// <param name="userId">用户ID</param>
        public OperationLog(string action, string resource, string description, int? userId = null)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("操作动作不能为空", nameof(action));
            
            Action = action;
            Resource = resource;
            Description = description;
            UserId = userId;
            Timestamp = DateTime.Now;
            CreateTime = DateTime.Now;
        }
        
        /// <summary>
        /// 设置资源ID
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        public void SetResourceId(int resourceId)
        {
            ResourceId = resourceId;
        }
        
        /// <summary>
        /// 设置网络信息
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="userAgent">用户代理</param>
        public void SetNetworkInfo(string ipAddress, string userAgent)
        {
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }
        
        /// <summary>
        /// 设置操作结果
        /// </summary>
        /// <param name="result">操作结果</param>
        public void SetResult(string result)
        {
            Result = result;
        }
        
        /// <summary>
        /// 设置成功结果
        /// </summary>
        public void SetSuccessResult()
        {
            Result = "Success";
        }
        
        /// <summary>
        /// 设置失败结果
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        public void SetFailureResult(string errorMessage)
        {
            Result = $"Failed: {errorMessage}";
        }
    }
} 