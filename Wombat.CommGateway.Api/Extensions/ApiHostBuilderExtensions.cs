using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Wombat.CommGateway.API.Options;
using Wombat.Infrastructure;
using Wombat.CommGateway.Infrastructure;

namespace Wombat.CommGateway.API.Extensions
{
    /// <summary>
    /// API主机配置扩展方法
    /// 封装主机配置逻辑
    /// </summary>
    public static class ApiHostBuilderExtensions
    {
        /// <summary>
        /// 配置API主机
        /// </summary>
        /// <param name="hostBuilder">主机构建器</param>
        /// <param name="configuration">配置对象</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>主机构建器</returns>
        public static IHostBuilder ConfigureApiHost(
            this IHostBuilder hostBuilder,
            IConfiguration configuration,
            Action<ApiOptions>? configureOptions = null)
        {
            // 绑定配置选项
            var apiOptions = ApiOptions.Bind(configuration);
            configureOptions?.Invoke(apiOptions);

            // 配置Serilog
            hostBuilder.ConfigureSerilog(apiOptions.Logging);

            // 配置缓存
            hostBuilder.ConfigureCache(apiOptions.Cache);

            // 配置ID生成器
            hostBuilder.ConfigureIdHelper();

            return hostBuilder;
        }

        /// <summary>
        /// 配置Serilog日志
        /// </summary>
        private static IHostBuilder ConfigureSerilog(
            this IHostBuilder hostBuilder,
            LoggingOptions loggingOptions)
        {
            SerilogHelper.Build();
            hostBuilder.UseSerilog();
            
            return hostBuilder;
        }

        /// <summary>
        /// 配置缓存
        /// </summary>
        private static IHostBuilder ConfigureCache(
            this IHostBuilder hostBuilder,
            Wombat.CommGateway.API.Options.CacheOptions cacheOptions)
        {
            hostBuilder.UseCache();
            return hostBuilder;
        }

        /// <summary>
        /// 配置ID生成器
        /// </summary>
        private static IHostBuilder ConfigureIdHelper(
            this IHostBuilder hostBuilder)
        {
            hostBuilder.UseIdHelper();
            return hostBuilder;
        }
    }
} 