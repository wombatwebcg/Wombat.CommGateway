using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;

namespace  Wombat.CommGateway.Infrastructure
{
    public static partial class Extention
    {

        /// <summary>
        /// 获取分页数据(包括总数量)
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="iEnumberable">数据源</param>
        /// <param name="pageInput">分页参数</param>
        /// <returns></returns>
        public static PageResult<T> GetPageResult<T>(this IEnumerable<T> iEnumberable, PageInput pageInput)
        {
            int count = iEnumberable.Count();

            var list = iEnumberable.AsQueryable()
                .OrderBy($@"{pageInput.SortField} {pageInput.SortType}")
                .Skip((pageInput.PageIndex - 1) * pageInput.PageRows)
                .Take(pageInput.PageRows)
                .ToList();

            return new PageResult<T> { Data = list, Total = count };
        }

        /// <summary>
        /// 获取分页数据(仅获取列表,不获取总数量)
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="iEnumberable">数据源</param>
        /// <param name="pageInput">分页参数</param>
        /// <returns></returns>
        public static List<T> GetPageList<T>(this IEnumerable<T> iEnumberable, PageInput pageInput)
        {
            var list = iEnumberable.AsQueryable()
                .OrderBy($@"{pageInput.SortField} {pageInput.SortType}")
                .Skip((pageInput.PageIndex - 1) * pageInput.PageRows)
                .Take(pageInput.PageRows)
                .ToList();

            return list;
        }

    }
}
