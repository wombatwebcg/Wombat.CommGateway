namespace Wombat.CommGateway.Domain.Enums
{
    /// <summary>
    /// 设备状态枚举
    /// </summary>
    public enum DeviceStatus
    {
        /// <summary>
        /// 已创建
        /// </summary>
        Created = 0,

        /// <summary>
        /// 运行中
        /// </summary>
        Running = 1,

        /// <summary>
        /// 已停止
        /// </summary>
        Stopped = 2,

        /// <summary>
        /// 错误
        /// </summary>
        Error = 3
    }
} 