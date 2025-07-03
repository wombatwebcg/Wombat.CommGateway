
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Wombat.CommGateway.API.Filters
{
    /// <summary>
    /// 返回结果不进行格式化
    /// </summary>
    /// 

    public class NoFormatResponseAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Action执行之前执行
        /// </summary>
        /// <param name="context">过滤器上下文</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {

        }

        /// <summary>
        /// Action执行完毕之后执行
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {

        }
    }
}