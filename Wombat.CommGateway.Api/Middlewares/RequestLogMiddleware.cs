﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Wombat;
using Wombat.Infrastructure;

namespace Wombat.CommGateway.API
{

    public class RequestLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestLogMiddleware(RequestDelegate next, ILogger<RequestLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        //[Autowired]
        public async Task Invoke(HttpContext context)
        {
            //context = ACServiceProvider.GetService<HttpContext>();

            Stopwatch watch = Stopwatch.StartNew();
            string resContent = string.Empty;

            //返回Body需特殊处理
            Stream originalResponseBody = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            try
            {
                await _next(context);

                memStream.Position = 0;
                resContent = new StreamReader(memStream).ReadToEnd();

                memStream.Position = 0;
                await memStream.CopyToAsync(originalResponseBody);
            }
            catch
            {
                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBody;

                watch.Stop();

                if (resContent?.Length > 1000)
                {
                    resContent += "......内容太长已忽略";
                }

                string log =
            @"方向:请求本系统
Url:{Url}
Time:{Time}ms
Method:{Method}
ContentType:{ContentType}
Body:{Body}
StatusCode:{StatusCode}

Response:{Response}
";
                _logger.LogInformation(
                    log,
                    context.Request.Path,
                    (int)watch.ElapsedMilliseconds,
                    context.Request.Method,
                    context.Request.ContentType,
                    context.RequestServices.GetRequiredService<RequestBody>()?.Body,
                    context.Response.StatusCode,
                    resContent
                    );
            }
        }
    }
}
