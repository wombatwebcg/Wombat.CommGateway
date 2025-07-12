using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic; // Added missing import
using Wombat.CommGateway.Application.Interfaces;

namespace Wombat.CommGateway.Application.Common.Logging
{
    /// <summary>
    /// 服务容器扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加数据库日志装饰器服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabaseLogger(this IServiceCollection services, 
            Action<DatabaseLoggerOptions> configureOptions = null)
        {
            // 注册配置选项
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<DatabaseLoggerOptions>(options => { });
            }

            // 注册装饰器工厂
            services.AddScoped(typeof(IApplicationLogger<>), typeof(ApplicationLogger<>));
            
            return services;
        }

        /// <summary>
        /// 为指定服务类型添加数据库日志装饰器
        /// </summary>
        /// <typeparam name="TService">服务类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabaseLoggerFor<TService>(this IServiceCollection services)
            where TService : class
        {
            // 移除原有的 ILogger<TService> 注册（如果存在）
            for (int i = services.Count - 1; i >= 0; i--)
            {
                var service = services[i];
                if (service.ServiceType == typeof(ILogger<TService>))
                {
                    services.RemoveAt(i);
                }
            }

            // 注册装饰器
            services.AddScoped<IApplicationLogger<TService>>(provider =>
            {
                var originalLogger = provider.GetService<ILogger<TService>>();
                var logService = provider.GetRequiredService<ILogService>();
                var options = provider.GetRequiredService<IOptions<DatabaseLoggerOptions>>();
                
                return new ApplicationLogger<TService>(originalLogger, logService, options);
            });

            return services;
        }

        /// <summary>
        /// 批量为多个服务类型添加数据库日志装饰器
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="serviceTypes">服务类型数组</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabaseLoggerForTypes(this IServiceCollection services, 
            params Type[] serviceTypes)
        {
            foreach (var serviceType in serviceTypes)
            {
                // 创建泛型类型 IApplicationLogger<T>
                var loggerType = typeof(IApplicationLogger<>).MakeGenericType(serviceType);
                var implementationType = typeof(ApplicationLogger<>).MakeGenericType(serviceType);
                
                // 注册装饰器
                services.AddScoped(loggerType, provider =>
                {
                    var originalLoggerType = typeof(ILogger<>).MakeGenericType(serviceType);
                    var originalLogger = provider.GetService(originalLoggerType);
                    var logService = provider.GetRequiredService<ILogService>();
                    var options = provider.GetRequiredService<IOptions<DatabaseLoggerOptions>>();
                    
                    return Activator.CreateInstance(implementationType, originalLogger, logService, options);
                });
            }

            return services;
        }

        /// <summary>
        /// 为所有已注册的服务添加数据库日志装饰器
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabaseLoggerForAll(this IServiceCollection services)
        {
            // 收集所有使用 ILogger<T> 的服务类型
            var loggerServiceTypes = new List<Type>();
            
            foreach (var service in services)
            {
                if (service.ServiceType.IsGenericType && 
                    service.ServiceType.GetGenericTypeDefinition() == typeof(ILogger<>))
                {
                    var genericArgs = service.ServiceType.GetGenericArguments();
                    if (genericArgs.Length == 1)
                    {
                        loggerServiceTypes.Add(genericArgs[0]);
                    }
                }
            }

            // 为每个服务类型添加装饰器
            return services.AddDatabaseLoggerForTypes(loggerServiceTypes.ToArray());
        }
    }
} 