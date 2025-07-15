using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wombat.CommGateway.API.Extensions;
using Wombat.CommGateway.API.Options;

namespace Wombat.CommGateway.API.Modules
{
    /// <summary>
    /// API模块
    /// 提供一站式配置入口，让Web项目能够一键启用所有API功能
    /// </summary>
    public static class ApiModule
    {
        /// <summary>
        /// 添加API模块到服务集合
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置对象</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddApiModule(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<ApiOptions>? configureOptions = null)
        {
            return services.AddApiServices(configuration, configureOptions);
        }

        /// <summary>
        /// 配置API模块主机
        /// </summary>
        /// <param name="hostBuilder">主机构建器</param>
        /// <param name="configuration">配置对象</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>主机构建器</returns>
        public static IHostBuilder ConfigureApiModule(
            this IHostBuilder hostBuilder,
            IConfiguration configuration,
            Action<ApiOptions>? configureOptions = null)
        {
            return hostBuilder.ConfigureApiHost(configuration, configureOptions);
        }

        /// <summary>
        /// 使用API模块
        /// </summary>
        /// <param name="app">应用程序构建器</param>
        /// <param name="configuration">配置对象</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>应用程序构建器</returns>
        public static IApplicationBuilder UseApiModule(
            this IApplicationBuilder app,
            IConfiguration configuration,
            Action<ApiOptions>? configureOptions = null)
        {
            return app.ConfigureApiPipeline(configuration, configureOptions);
        }

        /// <summary>
        /// 添加API模块（WebApplication扩展方法）
        /// </summary>
        /// <param name="app">Web应用程序</param>
        /// <param name="configuration">配置对象</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>Web应用程序</returns>
        public static WebApplication UseApiModule(
            this WebApplication app,
            IConfiguration configuration,
            Action<ApiOptions>? configureOptions = null)
        {
            app.ConfigureApiPipeline(configuration, configureOptions);
            return app;
        }

        /// <summary>
        /// 快速配置API模块（推荐使用）
        /// 在Web项目的Program.cs中调用此方法即可启用所有API功能
        /// </summary>
        /// <param name="builder">Web应用程序构建器</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>Web应用程序构建器</returns>
        public static WebApplicationBuilder AddApiModule(
            this WebApplicationBuilder builder,
            Action<ApiOptions>? configureOptions = null)
        {
            // 添加服务
            builder.Services.AddApiModule(builder.Configuration, configureOptions);
            
            // 配置主机
            builder.Host.ConfigureApiModule(builder.Configuration, configureOptions);
            
            return builder;
        }
    }
} 