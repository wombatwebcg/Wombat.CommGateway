using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    /// <summary>
    /// 调度的点位信息
    /// </summary>
    public class ScheduledPoint
    {
        public int PointId { get; set; }
        public int DeviceId { get; set; }
        public int ChannelId { get; set; }
        public string Address { get; set; }
        public DataType DataType { get; set; }
        public int ScanRate { get; set; }
        public DateTime NextExecutionTime { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public long ExecutionCount { get; set; }
        public long ErrorCount { get; set; }
    }
}
