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
        public async Task<ActionResult<Channel>> CreateChannel([FromBody] CreateChannelRequest request)
        {
            var channel = await _channelService.CreateAsync(new Application.DTOs.CreateChannelRequest(){ Name = request.Name, Type = request.Type, ProtocolConfigId = request.ProtocolConfigId });
            return Success(channel);
        }

        /// <summary>
        /// 更新通道状态
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateChannelStatus(int id, [FromBody] UpdateChannelStatusRequest request)
        {
            await _channelService.UpdateStatusAsync(id, request.Status);
            return Success();
        }

        /// <summary>
        /// 获取通道列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Channel>>> GetChannels()
        {
            var channels = await _channelService.GetListAsync();
            return Success(channels);
        }

        /// <summary>
        /// 获取通道详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Channel>> GetChannel(int id)
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
            await _channelService.UpdateAsync(id, new UpdateChannelRequest(){Configuration = configuration});
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

    public class CreateChannelRequest
    {
        public string Name { get; set; }
        public ChannelType Type { get; set; }
        public int ProtocolConfigId { get; set; }
    }

    public class UpdateChannelStatusRequest
    {
        public ChannelStatus Status { get; set; }
    }
} 