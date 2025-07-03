using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Application.Services;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 网关设备控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GatewayDeviceController : ApiControllerBase
    {
        private readonly IGatewayDeviceService _deviceService;

        public GatewayDeviceController(IGatewayDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        /// <summary>
        /// 获取所有设备
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<GatewayDeviceDto[]>> GetAll()
        {
            var devices = await _deviceService.GetAllAsync();
            return Success(devices);
        }

        /// <summary>
        /// 根据ID获取设备
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<GatewayDeviceDto>> GetById(int id)
        {
            var device = await _deviceService.GetByIdAsync(id);
            if (device == null)
            {
                return NotFound();
            }
            return Success(device);
        }

        /// <summary>
        /// 创建设备
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<GatewayDevice>> CreateDevice([FromBody] CreateDeviceRequest request)
        {
            var device = await _deviceService.CreateAsync(new CreateGatewayDeviceDto(){Name = request.Name, Description = request.Description, Type = request.Type});
            return Success(device);
        }

        /// <summary>
        /// 更新设备
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateGatewayDeviceDto dto)
        {
            await _deviceService.UpdateAsync(id, dto);
            return NoContent();
        }

        /// <summary>
        /// 删除设备
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _deviceService.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>
        /// 启动设备
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<IActionResult> Start(int id)
        {
            await _deviceService.StartAsync(id);
            return NoContent();
        }

        /// <summary>
        /// 停止设备
        /// </summary>
        [HttpPost("{id}/stop")]
        public async Task<IActionResult> Stop(int id)
        {
            await _deviceService.StopAsync(id);
            return NoContent();
        }

        /// <summary>
        /// 添加点位
        /// </summary>
        [HttpPost("{deviceId}/points")]
        public async Task<IActionResult> AddPoint(int deviceId, CreateDevicePointDto dto)
        {
            await _deviceService.AddPointAsync(deviceId, dto);
            return NoContent();
        }

        /// <summary>
        /// 更新点位
        /// </summary>
        [HttpPut("{deviceId}/points/{pointId}")]
        public async Task<IActionResult> UpdatePoint(int deviceId, int pointId, UpdateDevicePointDto dto)
        {
            await _deviceService.UpdatePointAsync(deviceId, pointId, dto);
            return NoContent();
        }

        /// <summary>
        /// 删除点位
        /// </summary>
        [HttpDelete("{deviceId}/points/{pointId}")]
        public async Task<IActionResult> DeletePoint(int deviceId, int pointId)
        {
            await _deviceService.DeletePointAsync(deviceId, pointId);
            return NoContent();
        }

        /// <summary>
        /// 更新设备状态
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateDeviceStatus(int id, [FromBody] UpdateDeviceStatusRequest request)
        {
            await _deviceService.UpdateDeviceStatusAsync(id, request.Status);
            return Success();
        }

        /// <summary>
        /// 获取设备列表
        /// </summary>
        [HttpGet("devices")]
        public async Task<ActionResult<IEnumerable<GatewayDevice>>> GetDevices()
        {
            var devices = await _deviceService.GetAllAsync();
            return Success(devices);
        }

        /// <summary>
        /// 更新设备属性
        /// </summary>
        [HttpPut("{id}/properties")]
        public async Task<IActionResult> UpdateDeviceProperties(int id, [FromBody] Dictionary<string, string> properties)
        {
            await _deviceService.UpdateAsync(id, new UpdateGatewayDeviceDto(){Properties = properties});
            return Success();
        }
    }

    public class CreateDeviceRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DeviceType Type { get; set; }
    }

    public class UpdateDeviceStatusRequest
    {
        public DeviceStatus Status { get; set; }
    }
} 