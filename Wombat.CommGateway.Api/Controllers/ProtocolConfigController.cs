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
        public async Task<ActionResult<List<ProtocolConfigDto>>> GetList()
        {
            var configs = await _protocolConfigService.GetListAsync();
            return Ok(configs);
        }

        /// <summary>
        /// 获取协议配置详情
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <returns>协议配置详情</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProtocolConfigDto>> GetById(int id)
        {
            var config = await _protocolConfigService.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound();
            }
            return Ok(config);
        }

        /// <summary>
        /// 创建协议配置
        /// </summary>
        /// <param name="request">创建请求</param>
        /// <returns>创建的协议配置</returns>
        [HttpPost]
        public async Task<ActionResult<ProtocolConfigDto>> Create([FromBody] CreateProtocolConfigRequest request)
        {
            var config = await _protocolConfigService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = config.Id }, config);
        }

        /// <summary>
        /// 更新协议配置
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <param name="request">更新请求</param>
        /// <returns>更新后的协议配置</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ProtocolConfigDto>> Update(int id, [FromBody] UpdateProtocolConfigRequest request)
        {
            var config = await _protocolConfigService.UpdateAsync(id, request);
            return Ok(config);
        }

        /// <summary>
        /// 删除协议配置
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <returns>是否删除成功</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> Delete(int id)
        {
            var result = await _protocolConfigService.DeleteAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return Ok(result);
        }

        /// <summary>
        /// 启用/禁用协议配置
        /// </summary>
        /// <param name="id">协议配置ID</param>
        /// <param name="isEnabled">是否启用</param>
        /// <returns>更新后的协议配置</returns>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<ProtocolConfigDto>> UpdateStatus(int id, [FromQuery] bool isEnabled)
        {
            var config = await _protocolConfigService.UpdateStatusAsync(id, isEnabled);
            return Ok(config);
        }
    }
}