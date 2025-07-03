using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wombat.Infrastructure;
using Wombat;
using Wombat.Extensions.DataTypeExtensions;
using System.IO;
using Wombat.CommGateway.API.Filters;



namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 基控制器
    /// </summary>
    /// 
    [FormatResponse]
    public class CustomController : ControllerBase
    {


        protected string GetAbsolutePath(string virtualPath)
        {
            string path = virtualPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (path[0] == '~')
                path = path.Remove(0, 2);

            string rootPath = HttpContext.RequestServices.GetService<IWebHostEnvironment>().WebRootPath;

            return Path.Combine(rootPath, path);
        }

        /// <summary>
        /// 返回html
        /// </summary>
        /// <param name="body">html内容</param>
        /// <returns></returns>
        protected ContentResult HtmlContent(string body)
        {
            return base.Content(body);
        }



        /// <summary>
        /// 返回JSON
        /// </summary>
        /// <param name="json">json字符串</param>
        /// <returns></returns>
        protected ContentResult JsonContent(string json)
        {
            return new ContentResult { Content = json, StatusCode = 200, ContentType = "application/json; charset=utf-8" };
        }

        /// <summary>
        /// 返回成功
        /// </summary>
        /// <returns></returns>
        protected ContentResult Success()
        {
            return JsonContent(new AjaxResult().ToLowercaseJson());
        }

        /// <summary>
        /// 返回成功
        /// </summary>
        /// <param name="msg">消息</param>
        /// <returns></returns>
        protected ContentResult Success(string msg)
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
        protected ContentResult Success<T>(T data)
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
        protected ContentResult Error()
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
        protected ContentResult Error(string msg)
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
        protected ContentResult Error(string msg, int errorCode)
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
