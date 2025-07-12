using Microsoft.Extensions.DependencyInjection;
using Wombat.CommGateway.Application.Common.Logging;
using Wombat.CommGateway.Application.Services;
using Wombat.CommGateway.Application.Services.DataCollection;

namespace Wombat.CommGateway.Application
{
    /// <summary>
    /// 应用层服务注册
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// 注册数据库日志装饰器服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabaseLoggerServices(this IServiceCollection services)
        {
            // 添加数据库日志装饰器基础服务
            services.AddDatabaseLogger(options =>
            {
                options.EnableDatabaseLogging = true;
                // 可以根据需要配置其他选项
            });

            // 方案1：为特定服务添加装饰器（推荐 - 精确控制）
            services.AddDatabaseLoggerFor<DeviceGroupService>()
                   .AddDatabaseLoggerFor<DeviceService>()
                   .AddDatabaseLoggerFor<DevicePointService>()
                   .AddDatabaseLoggerFor<ChannelService>()
                   .AddDatabaseLoggerFor<DataCollectionService>()
                   .AddDatabaseLoggerFor<WebSocketService>()
                   .AddDatabaseLoggerFor<RuleEngineService>()
                   .AddDatabaseLoggerFor<DataDistributionService>();

            // 方案2：批量添加装饰器（适用于大量服务）
            /*
            services.AddDatabaseLoggerForTypes(
                typeof(DeviceGroupService),
                typeof(DeviceService),
                typeof(DevicePointService),
                typeof(ChannelService),
                typeof(DataCollectionService),
                typeof(WebSocketService),
                typeof(RuleEngineService),
                typeof(DataDistributionService),
                typeof(ConnectionPoolManager),
                typeof(TimeWheelScheduler),
                typeof(CacheManager)
            );
            */

            // 方案3：为所有服务添加装饰器（慎用 - 性能影响）
            /*
            services.AddDatabaseLoggerForAll();
            */

            return services;
        }
    }
} 