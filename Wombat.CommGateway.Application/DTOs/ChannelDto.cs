using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 通信通道数据传输对象
    /// </summary>
    public class ChannelDto
    {
        /// <summary>
        /// 通道ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 通道类型
        /// </summary>
        public string ChannelType { get; set; }

        /// <summary>
        /// 通道配置
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 创建通信通道数据传输对象
    /// </summary>
    public class CreateChannelDto
    {
        /// <summary>
        /// 通道名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 通道类型
        /// </summary>
        public string ChannelType { get; set; }

        /// <summary>
        /// 通道配置
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 更新通信通道数据传输对象
    /// </summary>
    public class UpdateChannelDto
    {
        /// <summary>
        /// 通道ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 通道类型
        /// </summary>
        public string ChannelType { get; set; }

        /// <summary>
        /// 通道配置
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }


} 