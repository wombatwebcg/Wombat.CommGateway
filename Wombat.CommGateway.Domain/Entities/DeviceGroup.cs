using System;
using System.ComponentModel.DataAnnotations.Schema;
using Wombat.CommGateway.Domain.Common;

namespace Wombat.CommGateway.Domain.Entities
{
    /// <summary>
    /// 设备组实体
    /// </summary>
    [Table("DeviceGroups")]
    public class DeviceGroup : Entity
    {
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
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        private DeviceGroup() { }

        public DeviceGroup(string name, string description = "")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? "";
            CreateTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }

        public void UpdateInfo(string name, string description)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? "";
            UpdateTime = DateTime.Now;
        }
    }
} 