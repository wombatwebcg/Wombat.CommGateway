using CSRedis;
using Microsoft.Extensions.Caching.Distributed;
using Wombat.Infrastructure;
using  Wombat.CommGateway.Infrastructure;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;


namespace  Wombat.CommGateway.Infrastructure
{
    public static partial class Extention
    {
        /// <summary>
        /// 使用IdHelper
        /// </summary>
        /// <param name="hostBuilder">建造者</param>
        /// <returns></returns>
        public static IHostBuilder UseIdHelper(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((buidler, services) =>
            {
                services.AddSingleton(new SnowflakeHelper(0, 0));

            });

            return hostBuilder;
        }

        /// <summary>
        /// 使用缓存
        /// </summary>
        /// <param name="hostBuilder">建造者</param>
        /// <returns></returns>
        public static IHostBuilder UseCache(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices((buidlerContext, services) =>
            {
                var cacheOption = buidlerContext.Configuration.GetSection("Cache").Get<CacheOptions>();
                switch (cacheOption?.CacheType)
                {
                    case CacheType.Memory: services.AddDistributedMemoryCache(); break;
                    case CacheType.Redis:
                        {
                            var csredis = new CSRedisClient(cacheOption.RedisEndpoint);
                            RedisHelper.Initialization(csredis);
                            services.AddSingleton(csredis);
                            services.AddSingleton<IDistributedCache>(new CSRedisCache(RedisHelper.Instance));
                        }; break;
                    default:
                        SerilogHelper.LogException(new Exception("缓存类型无效"));
                        break;

                }
            });

            return hostBuilder;
        }



    }
}
