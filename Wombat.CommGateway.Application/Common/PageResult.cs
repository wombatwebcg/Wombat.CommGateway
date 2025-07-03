using System.Collections.Generic;

namespace Wombat.CommGateway.Application.Common
{
    /// <summary>
    /// 分页结果
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class PageResult<T>
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 当前页数据
        /// </summary>
        public List<T> Items { get; set; }
    }
} 