using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wombat.CommGateway.Infrastructure.Communication
{
    /// <summary>
    /// 协议接口
    /// </summary>
    public interface IProtocol
    {
        /// <summary>
        /// 协议名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 初始化协议
        /// </summary>
        Task InitializeAsync(Dictionary<string, string> configuration);

        /// <summary>
        /// 读取数据
        /// </summary>
        Task<object> ReadAsync(string address, string dataType);

        /// <summary>
        /// 写入数据
        /// </summary>
        Task WriteAsync(string address, string dataType, object value);

        /// <summary>
        /// 批量读取数据
        /// </summary>
        Task<Dictionary<string, object>> BatchReadAsync(Dictionary<string, string> addressDataTypeMap);

        /// <summary>
        /// 批量写入数据
        /// </summary>
        Task BatchWriteAsync(Dictionary<string, (string DataType, object Value)> addressValueMap);

        /// <summary>
        /// 连接设备
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// 断开连接
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// 获取连接状态
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 获取错误信息
        /// </summary>
        string GetLastError();
    }
} 