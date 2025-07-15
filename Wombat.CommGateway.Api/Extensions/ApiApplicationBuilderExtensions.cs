using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wombat.CommGateway.API.Options;
using Wombat.CommGateway.Application.Hubs;
using Wombat.CommGateway.Application.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Wombat.CommGateway.API.Controllers;

namespace Wombat.CommGateway.API.Extensions
{
    /// <summary>
    /// API应用程序构建器扩展方法
    /// 封装中间件配置逻辑
    /// </summary>
    public static class ApiApplicationBuilderExtensions
    {
        /// <summary>
        /// 配置API中间件管道
        /// </summary>
        /// <param name="app">应用程序构建器</param>
        /// <param name="configuration">配置对象</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>应用程序构建器</returns>
        public static IApplicationBuilder ConfigureApiPipeline(
            this IApplicationBuilder app,
            IConfiguration configuration,
            Action<ApiOptions>? configureOptions = null)
        {
            // 获取配置选项
            var apiOptions = ApiOptions.Bind(configuration);
            configureOptions?.Invoke(apiOptions);

            // 配置异常处理
            app.ConfigureExceptionHandling();

            // 配置WebSocket
            if (apiOptions.Middleware.EnableWebSocket)
            {
                app.UseWebSockets();
            }

            // 配置中间件
            app.ConfigureMiddlewares(apiOptions.Middleware);

            // 配置静态文件
            app.ConfigureStaticFiles();

            // 配置路由
            app.UseRouting();

            // 配置CORS
            if (apiOptions.Middleware.EnableCors)
            {
                app.UseCors(apiOptions.Middleware.CorsPolicyName);
            }

            // 配置HTTPS重定向
            app.UseHttpsRedirection();

            // 配置认证授权
            app.UseAuthentication();
            app.UseAuthorization();

            // 配置端点
            app.UseEndpoints(endpoints =>
            {
                // 配置控制器
                endpoints.MapControllers();

                // 配置SignalR Hub
                if (apiOptions.Middleware.EnableSignalR)
                {
                    endpoints.MapHub<DataCollectionHub>("/ws/datacollection")
                             .RequireCors(apiOptions.Middleware.CorsPolicyName);

                    endpoints.MapHub<LoggingHub>("/hubs/log")
                             .RequireCors(apiOptions.Middleware.CorsPolicyName);
                }

                // 配置WebSocket服务路由
                if (apiOptions.Middleware.EnableWebSocket)
                {
                    endpoints.Map("/ws/datacollection-ws", async context =>
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            var webSocketService = context.RequestServices.GetRequiredService<WebSocketService>();
                            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            var connectionId = Guid.NewGuid().ToString("N");
                            await webSocketService.HandleConnectionAsync(webSocket, connectionId);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                        }
                    });
                }


            });


            //app.MapGet("/vue", async context =>
            //{
            //    context.Response.ContentType = "text/html";
            //    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "vue", "index.html"));
            //});

            // 配置Swagger
            if (apiOptions.Middleware.EnableSwagger)
            {
                app.ConfigureSwagger();
            }
            app.UseDefaultFiles();

            return app;
        }

        /// <summary>
        /// 配置异常处理
        /// </summary>
        private static IApplicationBuilder ConfigureExceptionHandling(
            this IApplicationBuilder app)
        {
            var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            return app;
        }

        /// <summary>
        /// 配置中间件
        /// </summary>
        private static IApplicationBuilder ConfigureMiddlewares(
            this IApplicationBuilder app,
            MiddlewareOptions middlewareOptions)
        {
            if (middlewareOptions.EnableRequestBodyMiddleware)
            {
                app.UseMiddleware<RequestBodyMiddleware>();
            }

            if (middlewareOptions.EnableRequestLogMiddleware)
            {
                app.UseMiddleware<RequestLogMiddleware>();
            }

            return app;
        }

        /// <summary>
        /// 配置静态文件
        /// </summary>
        private static IApplicationBuilder ConfigureStaticFiles(
            this IApplicationBuilder app)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/octet-stream",

            });

            return app;
        }

        /// <summary>
        /// 配置Swagger
        /// </summary>
        private static IApplicationBuilder ConfigureSwagger(
            this IApplicationBuilder app)
        {
            app.UseOpenApi();
            app.UseSwaggerUi(config =>
            {
                // Swagger UI配置
            });

            return app;
        }
    }
} 