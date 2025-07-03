using System;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 设备组数据传输对象
    /// </summary>
    public class DeviceGroupDto
    {
        /// <summary>
        /// 设备组ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 设备组名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 设备组描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public string CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public string UpdateTime { get; set; }
    }

    /// <summary>
    /// 创建设备组数据传输对象
    /// </summary>
    public class CreateDeviceGroupDto
    {
        /// <summary>
        /// 设备组名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 设备组描述
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 更新设备组数据传输对象
    /// </summary>
    public class UpdateDeviceGroupDto
    {
        /// <summary>
        /// 设备组名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 设备组描述
        /// </summary>
        public string Description { get; set; }
    }
} 