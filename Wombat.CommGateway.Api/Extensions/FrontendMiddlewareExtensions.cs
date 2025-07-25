﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace Wombat.CommGateway.Api.Extensions
{
    public static class FrontendMiddlewareExtensions
    {
        public static IApplicationBuilder UseGatewayFrontend(this IApplicationBuilder app, string requestPath = "/gateway")
        {
            var vuePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "gateway");

            if (!Directory.Exists(vuePath))
            {
                throw new DirectoryNotFoundException($"Vue frontend directory not found at {vuePath}");
            }

            // 挂载静态资源路径（/vue/js/...、/vue/css/...）
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(vuePath),
                RequestPath = requestPath,
                OnPrepareResponse = ctx =>
                {
                    // 为静态文件添加缓存控制头
                    var path = ctx.File.PhysicalPath;
                    if (path != null)
                    {
                        var extension = Path.GetExtension(path).ToLowerInvariant();
                        
                        // 对于JS、CSS等静态资源，设置较长的缓存时间
                        if (extension == ".js" || extension == ".css" || extension == ".svg" || extension == ".png")
                        {
                            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
                        }
                        // 对于HTML文件，禁用缓存
                        else if (extension == ".html")
                        {
                            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                            ctx.Context.Response.Headers.Append("Expires", "0");
                        }
                    }
                }
            });


            // Vue Router fallback (404 -> index.html)
            app.Use(async (context, next) =>
            {
                await next();

                if (context.Response.StatusCode == 404 &&
                    context.Request.Path.StartsWithSegments(requestPath) &&
                    !Path.HasExtension(context.Request.Path))
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(Path.Combine(vuePath, "index.html"));
                }
            });






            //// 注入路由以便主项目注册 MapGet
            //app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                // 映射 /vue 到 index.html（首页入口）
                endpoints.MapGet(requestPath, async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(Path.Combine(vuePath, "index.html"));
                });

                endpoints.MapFallbackToFile("index.html", new StaticFileOptions
                 {
                     FileProvider = new PhysicalFileProvider(
                    Path.Combine(AppContext.BaseDirectory, "wwwroot", "gateway")
                )
                 });
            });
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value;

                if (!Path.HasExtension(path) &&
                    !path.StartsWith("/swagger") &&
                    !path.StartsWith("/api") &&
                    path.StartsWith("/gateway"))
                {
                    context.Request.Path = "/gateway/login";
                }

                await next();
            });
            return app;
        }
    }
}
