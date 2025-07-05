using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    /// <summary>
    /// 缓存的点位数据
    /// </summary>
    public class CachedPoint
    {
        public int PointId { get; set; }
        public string Value { get; set; }
        public DataPointStatus Status { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime LastFlushTime { get; set; }
        public bool IsDirty { get; set; }
    }
}
