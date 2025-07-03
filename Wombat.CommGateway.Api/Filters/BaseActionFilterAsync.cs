using Wombat.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;
using Wombat.CommGateway.API;
using Wombat;
using Wombat.Extensions.DataTypeExtensions;

namespace Wombat.CommGateway.API.Filters
{
    public abstract class BaseActionFilterAsync : Attribute,IAsyncActionFilter
    {
        public  virtual async  Task OnActionExecuting(ActionExecutingContext context)
        {
            await Task.CompletedTask;
        }

        public virtual async  Task OnActionExecuted(ActionExecutedContext context)
        {
            await Task.CompletedTask;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await OnActionExecuting(context);
            if (context.Result == null)
            {
                var nextContext = await next();
                await OnActionExecuted(nextContext);
            }
        }

        /// <summary>
        /// 返回JSON
        /// </summary>
        /// <param name="json">json字符串</param>
        /// <returns></returns>
        public ContentResult JsonContent(string json)
        {
            return new ContentResult { Content = json, StatusCode = 200, ContentType = "application/json; charset=utf-8" };
        }

        /// <summary>
        /// 返回成功
        /// </summary>
        /// <returns></returns>
        public ContentResult Success()
        {
            return JsonContent(new AjaxResult().ToLowercaseJson());
        }

        /// <summary>
        /// 返回成功
        /// </summary>
        /// <param name="msg">消息</param>
        /// <returns></returns>
        public ContentResult Success(string msg)
        {
            AjaxResult res = new AjaxResult
            {
                Message = msg
            };

            return JsonContent(res.ToLowercaseJson());
        }

        /// <summary>
        /// 返回成功
        /// </summary>
        /// <param name="message">返回的数据</param>
        /// <returns></returns>
        public ContentResult Success<T>(T data)
        {
            AjaxResult<T> res = new AjaxResult<T>
            {
                Message = "success",
                Data = data
            };

            return JsonContent(res.ToLowercaseJson());
        }

        /// <summary>
        /// 返回错误
        /// </summary>
        /// <returns></returns>
        public ContentResult Error()
        {
            AjaxResult res = new AjaxResult
            {
                Code = 400,
                Message = "fail"
            };

            return JsonContent(res.ToLowercaseJson());
        }

        /// <summary>
        /// 返回错误
        /// </summary>
        /// <param name="msg">错误提示</param>
        /// <returns></returns>
        public ContentResult Error(string msg)
        {
            AjaxResult res = new AjaxResult
            {
                Code = 400,
                Message = msg,
            };

            return JsonContent(res.ToLowercaseJson());
        }

        /// <summary>
        /// 返回错误
        /// </summary>
        /// <param name="msg">错误提示</param>
        /// <param name="errorCode">错误代码</param>
        /// <returns></returns>
        public ContentResult Error(string msg, int errorCode)
        {
            AjaxResult res = new AjaxResult
            {
                Message = msg,
                Code = errorCode
            };

            return JsonContent(res.ToLowercaseJson());
        }
    }
}