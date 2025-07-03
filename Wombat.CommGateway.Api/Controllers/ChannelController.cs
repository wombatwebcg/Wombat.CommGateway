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
    /// 通信通道控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChannelController : ApiControllerBase
    {
        private readonly IChannelService _channelService;

        public ChannelController(IChannelService channelService)
        {
            _channelService = channelService;
        }

        /// <summary>
        /// 创建通信通道
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ChannelDto>> CreateChannel([FromBody] CreateChannelDto dto)
        {
            var channel = await _channelService.CreateAsync(dto);
            return Success(channel);
        }


        /// <summary>
        /// 更新设备组
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<DeviceGroupDto>> UpdateDeviceGroup(int id, [FromBody] UpdateChannelDto dto)
        {
            var deviceGroup = await _channelService.UpdateAsync(id, dto);
            return Success(deviceGroup);
        }


        /// <summary>
        /// 更新通道状态
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateChannelStatus(int id, [FromBody] UpdateChannelStatusDto dto)
        {
            await _channelService.UpdateStatusAsync(id, dto.Status);
            return Success();
        }

        /// <summary>
        /// 更新通道启用状态
        /// </summary>
        [HttpPut("{id}/enable")]
        public async Task<IActionResult> UpdateChannelEnable(int id, [FromBody] UpdateChannelEnableDto dto)
        {
            await _channelService.UpdateEnableAsync(id, dto.Enable);
            return Success();
        }

        /// <summary>
        /// 获取通道列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChannelDto>>> GetChannels()
        {
            var channels = await _channelService.GetListAsync();
            return Success(channels);
        }

        /// <summary>
        /// 获取通道名称列表
        /// </summary>
        [HttpGet("nameList")]
        public async Task<ActionResult<List<string>>> GetChannelNameList()
        {
            var channels = await _channelService.GetListAsync();
            var nameList = channels.Select(c => c.Name).ToList();
            return Success(nameList);
        }

        /// <summary>
        /// 获取通道详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ChannelDto>> GetChannel(int id)
        {
            var channel = await _channelService.GetByIdAsync(id);
            if (channel == null)
                return NotFound();
            return Success(channel);
        }

        /// <summary>
        /// 更新通道配置
        /// </summary>
        [HttpPut("{id}/configuration")]
        public async Task<IActionResult> UpdateChannelConfiguration(int id, [FromBody] Dictionary<string, string> configuration)
        {
            await _channelService.UpdateConfigurationAsync(id, configuration);
            return Success();
        }

        /// <summary>
        /// 删除通道
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChannel(int id)
        {
            await _channelService.DeleteAsync(id);
            return Success();
        }

        /// <summary>
        /// 启动通道
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartChannel(int id)
        {
            await _channelService.StartAsync(id);
            return Success();
        }

        /// <summary>
        /// 停止通道
        /// </summary>
        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopChannel(int id)
        {
            await _channelService.StopAsync(id);
            return Success();
        }
    }
} 