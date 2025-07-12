using FreeSql.DataAnnotations;
using System;
using Wombat.CommGateway.Domain.Common;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 通信日志实体
    /// </summary>
    [Table(Name = "CommunicationLogs")]
    public class CommunicationLog : AggregateRoot
    {
        /// <summary>
        /// 通道ID
        /// </summary>
        public int? ChannelId { get; set; }
        
        /// <summary>
        /// 设备ID
        /// </summary>
        public int? DeviceId { get; set; }
        
        /// <summary>
        /// 通信方向
        /// </summary>
        public string Direction { get; set; }
        
        /// <summary>
        /// 通信协议
        /// </summary>
        public string Protocol { get; set; }
        
        /// <summary>
        /// 通信数据
        /// </summary>
        public string Data { get; set; }
        
        /// <summary>
        /// 通信状态
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// 响应时间（毫秒）
        /// </summary>
        public int? ResponseTime { get; set; }
        
        /// <summary>
        /// 通信时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        
        /// <summary>
        /// 关联通道
        /// </summary>
        [Navigate(nameof(ChannelId))]
        public Channel Channel { get; set; }
        
        /// <summary>
        /// 关联设备
        /// </summary>
        [Navigate(nameof(DeviceId))]
        public Device Device { get; set; }
        
        /// <summary>
        /// 私有构造函数
        /// </summary>
        private CommunicationLog() { }
        
        /// <summary>
        /// 创建通信日志
        /// </summary>
        /// <param name="direction">通信方向</param>
        /// <param name="protocol">通信协议</param>
        /// <param name="data">通信数据</param>
        /// <param name="channelId">通道ID</param>
        /// <param name="deviceId">设备ID</param>
        public CommunicationLog(string direction, string protocol, string data, int? channelId = null, int? deviceId = null)
        {
            if (string.IsNullOrWhiteSpace(direction))
                throw new ArgumentException("通信方向不能为空", nameof(direction));
            
            Direction = direction;
            Protocol = protocol;
            Data = data;
            ChannelId = channelId;
            DeviceId = deviceId;
            Timestamp = DateTime.Now;
            CreateTime = DateTime.Now;
        }
        
        /// <summary>
        /// 设置通信状态为成功
        /// </summary>
        /// <param name="responseTime">响应时间（毫秒）</param>
        public void SetSuccess(int responseTime = 0)
        {
            Status = "Success";
            ResponseTime = responseTime;
        }
        
        /// <summary>
        /// 设置通信状态为失败
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="responseTime">响应时间（毫秒）</param>
        public void SetFailure(string errorMessage, int responseTime = 0)
        {
            Status = "Failed";
            ErrorMessage = errorMessage;
            ResponseTime = responseTime;
        }
        
        /// <summary>
        /// 设置通信状态为超时
        /// </summary>
        /// <param name="responseTime">响应时间（毫秒）</param>
        public void SetTimeout(int responseTime)
        {
            Status = "Timeout";
            ResponseTime = responseTime;
        }
        
        /// <summary>
        /// 创建发送日志
        /// </summary>
        /// <param name="protocol">通信协议</param>
        /// <param name="data">发送数据</param>
        /// <param name="channelId">通道ID</param>
        /// <param name="deviceId">设备ID</param>
        /// <returns>通信日志实例</returns>
        public static CommunicationLog CreateSendLog(string protocol, string data, int? channelId = null, int? deviceId = null)
        {
            return new CommunicationLog("Send", protocol, data, channelId, deviceId);
        }
        
        /// <summary>
        /// 创建接收日志
        /// </summary>
        /// <param name="protocol">通信协议</param>
        /// <param name="data">接收数据</param>
        /// <param name="channelId">通道ID</param>
        /// <param name="deviceId">设备ID</param>
        /// <returns>通信日志实例</returns>
        public static CommunicationLog CreateReceiveLog(string protocol, string data, int? channelId = null, int? deviceId = null)
        {
            return new CommunicationLog("Receive", protocol, data, channelId, deviceId);
        }
    }
} 