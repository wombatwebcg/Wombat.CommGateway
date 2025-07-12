namespace Wombat.CommGateway.Domain.Enums
{
    /// <summary>
    /// 日志分类枚举
    /// </summary>
    public enum LogCategory
    {
        /// <summary>
        /// 系统日志 - 系统内部事件
        /// </summary>
        System = 0,
        
        /// <summary>
        /// 操作日志 - 用户操作记录
        /// </summary>
        Operation = 1,
        
        /// <summary>
        /// 通信日志 - 设备通信记录
        /// </summary>
        Communication = 2,
        
        /// <summary>
        /// 性能日志 - 性能监控记录
        /// </summary>
        Performance = 3,
        
        /// <summary>
        /// 安全日志 - 安全相关事件
        /// </summary>
        Security = 4,
        
        /// <summary>
        /// 审计日志 - 审计相关记录
        /// </summary>
        Audit = 5,
        
        /// <summary>
        /// 数据采集日志 - 数据采集相关记录
        /// </summary>
        DataCollection = 6
    }
} 