﻿using System.Collections.Generic;

namespace  Wombat.CommGateway.Infrastructure
{
    /// <summary>
    /// 分页返回结果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PageResult<T> : AjaxResult<List<T>>
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        public int Total { get; set; }
    }
}
