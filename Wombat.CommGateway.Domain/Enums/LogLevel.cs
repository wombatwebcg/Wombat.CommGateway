namespace Wombat.CommGateway.Domain.Enums
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 跟踪级别 - 最详细的日志信息
        /// </summary>
        Trace = 0,
        
        /// <summary>
        /// 调试级别 - 调试信息
        /// </summary>
        Debug = 1,
        
        /// <summary>
        /// 信息级别 - 一般信息
        /// </summary>
        Information = 2,
        
        /// <summary>
        /// 警告级别 - 警告信息
        /// </summary>
        Warning = 3,
        
        /// <summary>
        /// 错误级别 - 错误信息
        /// </summary>
        Error = 4,
        
        /// <summary>
        /// 严重级别 - 严重错误信息
        /// </summary>
        Critical = 5
    }
} 