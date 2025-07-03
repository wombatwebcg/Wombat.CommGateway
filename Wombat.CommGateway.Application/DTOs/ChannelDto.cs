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
        public int Type { get; set; }

        /// <summary>
        /// 通道协议
        /// </summary>
        public int Protocol { get; set; }

        /// <summary>
        /// 通道角色
        /// </summary>
        public int Role { get; set; }

        /// <summary>
        /// 通道状态
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 通道配置
        /// </summary>
        public Dictionary<string, string> Configuration { get; set; }
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
        public int Type { get; set; }

        /// <summary>
        /// 通道协议
        /// </summary>
        public int Protocol { get; set; }

        /// <summary>
        /// 通道角色
        /// </summary>
        public int Role { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 通道状态
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 通道配置
        /// </summary>
        public Dictionary<string, string> Configuration { get; set; }
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
        public int Type { get; set; }

        /// <summary>
        /// 通道协议
        /// </summary>
        public int Protocol { get; set; }

        /// <summary>
        /// 通道角色
        /// </summary>
        public int? Role { get; set; }

        /// <summary>
        /// 通道配置
        /// </summary>
        public Dictionary<string, string> Configuration { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; }
    }

    public class UpdateChannelStatusDto
    {
        public int Status { get; set; }
    }

    public class UpdateChannelEnableDto
    {
        public bool Enable { get; set; }
    }

    public class ChannelNameListDto
    {
        public string Name { get; set; }
    }
} 