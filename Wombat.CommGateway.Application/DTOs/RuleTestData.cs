using System;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 规则测试数据
    /// </summary>
    public class RuleTestData
    {
        /// <summary>
        /// 触发值
        /// </summary>
        public string TriggerValue { get; set; }

        /// <summary>
        /// 数据点ID
        /// </summary>
        public int PointId { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}