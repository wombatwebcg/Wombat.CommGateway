using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Services;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;

namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 协议配置控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProtocolConfigController : ApiControllerBase
    {
        private readonly IProtocolConfigService _protocolConfigService;

        public ProtocolConfigController(IProtocolConfigService protocolConfigService)
        {
            _protocolConfigService = protocolConfigService;
        }

        /// <summary>
        /// 获取协议配置列表
        /// </summary>
        /// <returns>协议配置列表</returns>
        [HttpGet]
        public async Task<ActionResult<List<ProtocolConfigDto>>> GetProtocolConfigList()
        {
            var configs = await _protocolConfigService.GetListAsync();
            return Success(configs);
        }

        /// <summary>
        /// 获取协议配置详情
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <returns>协议配置详情</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProtocolConfigDto>> GetProtocolConfigById(int id)
        {
            var config = await _protocolConfigService.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound();
            }
            return Success(config);
        }

        /// <summary>
        /// 创建协议配置
        /// </summary>
        /// <param name="dto">创建请求</param>
        /// <returns>创建的协议配置</returns>
        [HttpPost]
        public async Task<ActionResult<ProtocolConfigDto>> CreateProtocolConfig([FromBody] CreateProtocolConfigDto dto)
        {
            var config = await _protocolConfigService.CreateAsync(dto);
            return Success(config);
        }

        /// <summary>
        /// 更新协议配置
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <param name="dto">更新请求</param>
        /// <returns>更新后的协议配置</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ProtocolConfigDto>> UpdateProtocolConfig(int id, [FromBody] UpdateProtocolConfigDto dto)
        {
            var config = await _protocolConfigService.UpdateAsync(id, dto);
            return Success(config);
        }

        /// <summary>
        /// 删除协议配置
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <returns>是否删除成功</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteProtocolConfig(int id)
        {
            var result = await _protocolConfigService.DeleteAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return Success(result);
        }

        /// <summary>
        /// 启用/禁用协议配置
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <param name="enabled">是否启用</param>
        /// <returns>更新后的协议配置</returns>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<ProtocolConfigDto>> UpdateProtocolConfigStatus(int id, [FromQuery] bool enabled)
        {
            var config = await _protocolConfigService.UpdateStatusAsync(id, enabled);
            return Success(config);
        }
    }
}