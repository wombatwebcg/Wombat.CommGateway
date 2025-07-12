using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
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
using OpenApiSecurityScheme = NSwag.OpenApiSecurityScheme;
using Wombat.CommGateway.Application.Mappings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wombat.CommGateway.Application.Services;
using Wombat.CommGateway.Infrastructure.Communication;
using NPOI.XWPF.UserModel;
using Microsoft.AspNetCore.SignalR;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Application.Hubs;
using Wombat.CommGateway.Application.Services.DataCollection;
using Wombat.CommGateway.Application.Common.Logging;



namespace Wombat.CommGateway.API
{
    public class Program
    {

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            SerilogHelper.Build();
            builder.Host.UseSerilog();
            builder.Services.AddLogging(build =>
            {
                object p = build.AddSerilog(logger: SerilogHelper.Log);
            });

            builder.Services.AddOptions();
            

            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("jwt"));
            builder.Services.AddJwt(builder.Configuration);
            
            builder.Services.Configure<FreeSqlCollectionConfig>(builder.Configuration.GetSection("SqlConfig"));
            builder.Services.AddFreeSqlRepository<GatawayDB>();
            builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

            builder.Services.AddControllers(options =>
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


            builder.Services.AddHttpContextAccessor();

            #region HttpContext MemoryCache

            builder.Host.UseIdHelper();

            builder.Host.UseCache();

            #endregion

            builder.Services.AddAuthorization();

            builder.Services.AddEndpointsApiExplorer();


            #region Swagger
            builder.Services.AddOpenApiDocument(config =>
            {
                config.Title = "Gateway.WebApi";
                config.Description = "API";
                config.AllowNullableBodyParameters = true;
                config.AddSecurity("Token", Enumerable.Empty<string>(), new OpenApiSecurityScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                    //Authorize
                    //Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
                    Type = OpenApiSecuritySchemeType.Http,

                    // bearer
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
            });

            #endregion

            builder.Services.AddScoped(typeof(IApplicationLogger<>), typeof(ApplicationLogger<>));
            Application.AutoInjectExtension.AddAutoInject(builder.Services);
            Infrastructure.AutoInjectExtension.AddAutoInject(builder.Services);
            Api.AutoInjectExtension.AddAutoInject(builder.Services);

            // 注册AutoMapper
            builder.Services.AddAutoMapper(typeof(AutoMapperProfile));



            // 手动注册RequestBody服务
            //builder.Services.AddScoped<RequestBody>();

            //builder.Services.AddScoped<Wombat.CommGateway.API.RequestBody>();
            builder.Services.AddHostedService<DataCollectionService>();

            // 注册SignalR服务，配置WebSocket传输
            builder.Services.AddSignalR(options =>
            {
                // 配置客户端超时
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                
                // 启用详细日志
                options.EnableDetailedErrors = true;
            });

            // 注册WebSocket服务


            // 注册DataCollectionHubService为ICacheUpdateNotificationService实现
            //builder.Services.AddSingleton<ICacheUpdateNotificationService, DataCollectionHubService>();

            // 添加SignalR的CORS策略
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("SignalRPolicy", policy =>
                {
                    policy.SetIsOriginAllowed(origin => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");

            }

            //app.UseCors(x =>
            //{
            //    x.AllowAnyOrigin()
            //    .AllowAnyHeader()
            //    .AllowAnyMethod()
            //    .DisallowCredentials();

            //}).UseForwardedHeaders(new ForwardedHeadersOptions
            //{
            //    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            //});


            app.UseWebSockets();
            app.UseMiddleware<RequestBodyMiddleware>();
            app.UseMiddleware<RequestLogMiddleware>();
            // 启用WebSocket桥接中间件，使用独立路径避免与SignalR冲突
            //app.UseMiddleware<WsBridgeMiddleware>();

            // 配置WebSocket服务路由
            app.Map("/ws/datacollection-ws", async context =>
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

            app.UseStaticFiles(new StaticFileOptions
                {
                    ServeUnknownFileTypes = true,
                    DefaultContentType = "application/octet-stream"
                })
                .UseRouting();


            app.UseCors("SignalRPolicy");

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseStaticFiles();

            app.MapControllers();


            // 注册SignalR Hub路由，使用特定的CORS策略，强制WebSocket传输
// 注意：WsBridgeMiddleware使用/ws/bridge路径，SignalR使用/ws/datacollection路径，避免冲突
app.MapHub<DataCollectionHub>("/ws/datacollection")
   .RequireCors("SignalRPolicy");

// 注册日志Hub路由
app.MapHub<LoggingHub>("/hubs/log")
   .RequireCors("SignalRPolicy");


            #region Swagger
            app.UseOpenApi();
            app.UseSwaggerUi(config =>
            {

            });
            #endregion



            app.Run();

        }




    }
}
