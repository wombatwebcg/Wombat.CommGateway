using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSwag;
using Serilog;
using Wombat.Extensions.DataTypeExtensions;
using Wombat.Extensions.FreeSql;
using Wombat.Extensions.FreeSql.Config;
using Wombat.CommGateway.API.Filters;
using Wombat.CommGateway.Domain;
using Wombat.CommGateway.Infrastructure;
using Wombat.Infrastructure;
using Wombat.CommGateway.Application.Mappings;
using Wombat.CommGateway.Application.Services;
using Wombat.CommGateway.Infrastructure.Communication;
using Microsoft.AspNetCore.SignalR;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Application.Hubs;
using Wombat.CommGateway.Application.Services.DataCollection;
using Wombat.CommGateway.Application.Common.Logging;
using Wombat.CommGateway.API.Options;
using OpenApiSecurityScheme = NSwag.OpenApiSecurityScheme;

namespace Wombat.CommGateway.API.Extensions
{
    /// <summary>
    /// API服务注册扩展方法
    /// 封装所有服务注册逻辑
    /// </summary>
    public static class ApiServiceCollectionExtensions
    {
        /// <summary>
        /// 添加API服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置对象</param>
        /// <param name="configureOptions">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddApiServices(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<ApiOptions>? configureOptions = null)
        {
            // 绑定配置选项
            var apiOptions = ApiOptions.Bind(configuration);
            configureOptions?.Invoke(apiOptions);
            
            // 注册配置选项
            services.Configure<ApiOptions>(options =>
            {
                options.Jwt = apiOptions.Jwt;
                options.Database = apiOptions.Database;
                options.Logging = apiOptions.Logging;
                options.Middleware = apiOptions.Middleware;
                options.Cache = apiOptions.Cache;
            });

            // 注册基础服务
            services.AddOptions();
            services.AddHttpContextAccessor();
            // 注册JWT认证
            services.Configure<JwtOptions>(configuration.GetSection("jwt"));
            services.AddJwtAuthentication(apiOptions.Jwt);

            // 注册数据库服务
            services.AddDatabaseServices(apiOptions.Database, configuration);

            // 注册控制器和JSON序列化
            services.AddControllersWithJsonSerialization();

            // 注册授权
            services.AddAuthorization();

            // 注册API文档
            if (apiOptions.Middleware.EnableSwagger)
            {
                services.AddSwaggerDocumentation();
            }

            // 注册自动注入服务
            services.AddAutoInjectedServices();

            // 注册AutoMapper
            services.AddAutoMapper(typeof(AutoMapperProfile));

            // 注册后台服务
            services.AddHostedService<DataCollectionService>();

            // 注册SignalR服务
            if (apiOptions.Middleware.EnableSignalR)
            {
                services.AddSignalRServices();
            }

            // 注册CORS策略
            if (apiOptions.Middleware.EnableCors)
            {
                services.AddCorsPolicy(apiOptions.Middleware.CorsPolicyName);
            }

            return services;
        }

        /// <summary>
        /// 添加JWT认证服务
        /// </summary>
        private static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            Wombat.CommGateway.API.Options.JwtOptions jwtOptions)
        {
            services.Configure<Wombat.CommGateway.Infrastructure.JwtOptions>(options =>
            {
                options.Secret = jwtOptions.Secret;
                options.AccessExpireHours = jwtOptions.AccessExpireHours;
                options.RefreshExpireHours = jwtOptions.RefreshExpireHours;
            });

            // 使用配置对象创建临时配置
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["jwt:secret"] = jwtOptions.Secret,
                ["jwt:accessExpireHours"] = jwtOptions.AccessExpireHours.ToString(),
                ["jwt:refreshExpireHours"] = jwtOptions.RefreshExpireHours.ToString()
            });
            var tempConfig = configBuilder.Build();
            
            services.AddJwt(tempConfig);
            return services;
        }

        /// <summary>
        /// 添加数据库服务
        /// </summary>
        private static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            DatabaseOptions databaseOptions,
            IConfiguration configuration)
        {
            // 直接绑定FreeSql配置
            services.Configure<FreeSqlCollectionConfig>(configuration.GetSection("SqlConfig"));
            services.AddFreeSqlRepository<GatawayDB>();
            return services;
        }

        /// <summary>
        /// 添加控制器和JSON序列化
        /// </summary>
        private static IServiceCollection AddControllersWithJsonSerialization(
            this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options => 
                options.SuppressModelStateInvalidFilter = true);

            services.AddControllers(options =>
            {
                options.Filters.Add<ValidFilterAttribute>();
                options.Filters.Add<GlobalExceptionFilter>();
                options.Filters.Add<FormatResponseAttribute>();
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new LowercaseContractResolver();
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                options.SerializerSettings.GetType().GetProperties().ForEach(aProperty =>
                {
                    var value = aProperty.GetValue(new JsonSerializerSettings
                    {
                        ContractResolver = new DefaultContractResolver(),
                        DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                        DateFormatString = "yyyy-MM-dd HH:mm:ss.fff"
                    });
                    aProperty.SetValue(options.SerializerSettings, value);
                });
            });

            return services;
        }

        /// <summary>
        /// 添加Swagger文档
        /// </summary>
        private static IServiceCollection AddSwaggerDocumentation(
            this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddOpenApiDocument(config =>
            {
                config.Title = "Gateway.WebApi";
                config.Description = "API";
                config.AllowNullableBodyParameters = true;
                config.AddSecurity("Token", Enumerable.Empty<string>(), new OpenApiSecurityScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                    Type = OpenApiSecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
            });

            return services;
        }

        /// <summary>
        /// 添加自动注入服务
        /// </summary>
        private static IServiceCollection AddAutoInjectedServices(
            this IServiceCollection services)
        {
            Application.AutoInjectExtension.AddAutoInject(services);
            Infrastructure.AutoInjectExtension.AddAutoInject(services);
            Api.AutoInjectExtension.AddAutoInject(services);
            return services;
        }

        /// <summary>
        /// 添加SignalR服务
        /// </summary>
        private static IServiceCollection AddSignalRServices(
            this IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.EnableDetailedErrors = true;
            });

            return services;
        }

        /// <summary>
        /// 添加CORS策略
        /// </summary>
        private static IServiceCollection AddCorsPolicy(
            this IServiceCollection services,
            string policyName)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(policyName, policy =>
                {
                    policy.SetIsOriginAllowed(origin => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }
    }
} 