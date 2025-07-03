using System.Collections.Generic;

namespace Wombat.CommGateway.Infrastructure.Communication
{
    /// <summary>
    /// 协议工厂接口
    /// </summary>
    public interface IProtocolFactory
    {
        /// <summary>
        /// 创建协议实例
        /// </summary>
        IProtocol CreateProtocol(string protocolType, Dictionary<string, string> configuration);

        /// <summary>
        /// 获取支持的协议类型列表
        /// </summary>
        IEnumerable<string> GetSupportedProtocolTypes();
    }
} 