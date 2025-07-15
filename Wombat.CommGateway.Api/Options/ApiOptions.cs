using Microsoft.Extensions.Configuration;

namespace Wombat.CommGateway.API.Options
{
    /// <summary>
    /// API主配置类
    /// 包含所有子配置选项
    /// </summary>
    public class ApiOptions
    {
        /// <summary>
        /// JWT认证配置
        /// </summary>
        public JwtOptions Jwt { get; set; } = new();

        /// <summary>
        /// 数据库配置
        /// </summary>
        public DatabaseOptions Database { get; set; } = new();

        /// <summary>
        /// 日志配置
        /// </summary>
        public LoggingOptions Logging { get; set; } = new();

        /// <summary>
        /// 中间件配置
        /// </summary>
        public MiddlewareOptions Middleware { get; set; } = new();

        /// <summary>
        /// 缓存配置
        /// </summary>
        public CacheOptions Cache { get; set; } = new();

        /// <summary>
        /// 从配置节绑定选项
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <returns>绑定后的选项</returns>
        public static ApiOptions Bind(IConfiguration configuration)
        {
            var options = new ApiOptions();
            
            // 绑定JWT配置
            configuration.GetSection("jwt").Bind(options.Jwt);
            
            // 绑定数据库配置
            configuration.GetSection("SqlConfig").Bind(options.Database);
            
            // 绑定缓存配置
            configuration.GetSection("Cache").Bind(options.Cache);
            
            // 绑定日志配置（如果有专门的日志配置节）
            configuration.GetSection("Logging").Bind(options.Logging);
            
            // 绑定中间件配置（如果有专门的中间件配置节）
            configuration.GetSection("Middleware").Bind(options.Middleware);
            
            return options;
        }
    }

    /// <summary>
    /// JWT认证配置
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// JWT密钥
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// 访问令牌过期时间（小时）
        /// </summary>
        public int AccessExpireHours { get; set; } = 24;

        /// <summary>
        /// 刷新令牌过期时间（小时）
        /// </summary>
        public int RefreshExpireHours { get; set; } = 24;
    }

    /// <summary>
    /// 数据库配置
    /// </summary>
    public class DatabaseOptions
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 数据库类型
        /// </summary>
        public string DbType { get; set; } = "Sqlite";

        /// <summary>
        /// 是否启用自动迁移
        /// </summary>
        public bool AutoMigration { get; set; } = true;
    }

    /// <summary>
    /// 日志配置
    /// </summary>
    public class LoggingOptions
    {
        /// <summary>
        /// 是否启用控制台日志
        /// </summary>
        public bool EnableConsole { get; set; } = true;

        /// <summary>
        /// 是否启用文件日志
        /// </summary>
        public bool EnableFile { get; set; } = true;

        /// <summary>
        /// 是否启用数据库日志
        /// </summary>
        public bool EnableDatabase { get; set; } = true;

        /// <summary>
        /// 日志级别
        /// </summary>
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string FilePath { get; set; } = "logs/app-.log";
    }

    /// <summary>
    /// 中间件配置
    /// </summary>
    public class MiddlewareOptions
    {
        /// <summary>
        /// 是否启用请求体中间件
        /// </summary>
        public bool EnableRequestBodyMiddleware { get; set; } = true;

        /// <summary>
        /// 是否启用请求日志中间件
        /// </summary>
        public bool EnableRequestLogMiddleware { get; set; } = true;

        /// <summary>
        /// 是否启用WebSocket支持
        /// </summary>
        public bool EnableWebSocket { get; set; } = true;

        /// <summary>
        /// 是否启用SignalR
        /// </summary>
        public bool EnableSignalR { get; set; } = true;

        /// <summary>
        /// 是否启用Swagger文档
        /// </summary>
        public bool EnableSwagger { get; set; } = true;

        /// <summary>
        /// 是否启用CORS
        /// </summary>
        public bool EnableCors { get; set; } = true;

        /// <summary>
        /// CORS策略名称
        /// </summary>
        public string CorsPolicyName { get; set; } = "SignalRPolicy";
    }

    /// <summary>
    /// 缓存配置
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// 缓存类型
        /// </summary>
        public string CacheType { get; set; } = "Memory";

        /// <summary>
        /// Redis连接字符串
        /// </summary>
        public string RedisEndpoint { get; set; } = string.Empty;
    }
} 