using System;
using System.Collections.Generic;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.DTOs
{
    /// <summary>
    /// 协议配置数据传输对象
    /// </summary>
    public class ProtocolConfigDto
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 协议名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 协议类型
        /// </summary>
        public string ProtocolType { get; set; }

        /// <summary>
        /// 协议配置
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



        /// <summary>
        /// 协议类型
        /// </summary>
        public ProtocolType Type { get; set; }

        /// <summary>
        /// 协议版本
        /// </summary>
        public string Version { get;set; }

        /// <summary>
        /// 协议参数
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }

    }

    /// <summary>
    /// 创建协议配置数据传输对象
    /// </summary>
    public class CreateProtocolConfigDto
    {
        /// <summary>
        /// 协议名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 协议类型
        /// </summary>
        public string ProtocolType { get; set; }

        /// <summary>
        /// 协议配置
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
    /// 更新协议配置数据传输对象
    /// </summary>
    public class UpdateProtocolConfigDto
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 协议名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 协议类型
        /// </summary>
        public string ProtocolType { get; set; }

        /// <summary>
        /// 协议配置
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
    /// 创建协议配置请求
    /// </summary>
    public class CreateProtocolConfigRequest
    {
        /// <summary>
        /// 协议名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 协议类型
        /// </summary>
        public ProtocolType Type { get; set; }

        /// <summary>
        /// 协议版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 协议参数
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }
    }

    /// <summary>
    /// 更新协议配置请求
    /// </summary>
    public class UpdateProtocolConfigRequest
    {
        /// <summary>
        /// 协议名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 协议版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 协议参数
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }
    }
} 