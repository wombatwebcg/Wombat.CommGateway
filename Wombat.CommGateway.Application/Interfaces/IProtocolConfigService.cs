using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.DTOs;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 协议配置服务接口
    /// </summary>
    public interface IProtocolConfigService
    {
        /// <summary>
        /// 获取协议配置列表
        /// </summary>
        /// <returns>协议配置列表</returns>
        Task<List<ProtocolConfigDto>> GetListAsync();

        /// <summary>
        /// 获取协议配置详情
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <returns>协议配置详情</returns>
        Task<ProtocolConfigDto> GetByIdAsync(int id);

        /// <summary>
        /// 创建协议配置
        /// </summary>
        /// <param name="dto">创建请求</param>
        /// <returns>创建的协议配置</returns>
        Task<ProtocolConfigDto> CreateAsync(CreateProtocolConfigDto dto);

        /// <summary>
        /// 更新协议配置
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <param name="dto">更新请求</param>
        /// <returns>更新后的协议配置</returns>
        Task<ProtocolConfigDto> UpdateAsync(int id, UpdateProtocolConfigDto dto);

        /// <summary>
        /// 删除协议配置
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 启用/禁用协议配置
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <param name="enabled">是否启用</param>
        /// <returns>更新后的协议配置</returns>
        Task<ProtocolConfigDto> UpdateStatusAsync(int id, bool enabled);
    }
} 