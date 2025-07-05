using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    /// <summary>
    /// 连接池统计信息
    /// </summary>
    public class ConnectionPoolStatistics
    {
        /// <summary>
        /// 通道ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 最大连接池大小
        /// </summary>
        public int MaxPoolSize { get; set; }

        /// <summary>
        /// 可用连接数
        /// </summary>
        public int AvailableConnections { get; set; }

        /// <summary>
        /// 活跃连接数
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// 总连接数
        /// </summary>
        public int TotalConnections { get; set; }

        /// <summary>
        /// 连接池利用率
        /// </summary>
        public double PoolUtilization { get; set; }

        /// <summary>
        /// 最后健康检查时间
        /// </summary>
        public DateTime LastHealthCheckTime { get; set; }

        /// <summary>
        /// 获取统计信息字符串
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public override string ToString()
        {
            return $"通道 {ChannelId}: 总连接 {TotalConnections}/{MaxPoolSize}, " +
                   $"可用 {AvailableConnections}, 活跃 {ActiveConnections}, " +
                   $"利用率 {PoolUtilization:P1}, 最后检查 {LastHealthCheckTime:yyyy-MM-dd HH:mm:ss}";
        }
    }

}
