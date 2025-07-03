using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;

namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 设备控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ApiControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DeviceController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        /// <summary>
        /// 获取设备列表（分页）
        /// </summary>
        [HttpGet("list")]
        public async Task<ActionResult<DeviceResponseDto>> GetDevices([FromQuery] DeviceQueryDto query)
        {
            var result = await _deviceService.GetDevicesAsync(query);
            return Success(result);
        }

        /// <summary>
        /// 获取所有设备（不分页）
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<DeviceDto>>> GetAllDevices()
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            return Success(devices);
        }

        /// <summary>
        /// 获取设备详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DeviceDto>> GetDeviceById(int id)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
                return NotFound();
            return Success(device);
        }

        /// <summary>
        /// 创建设备
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<int>> CreateDevice([FromBody] CreateDeviceDto dto)
        {
            var id = await _deviceService.CreateDeviceAsync(dto);
            return Success(id);
        }

        /// <summary>
        /// 更新设备
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] UpdateDeviceDto dto)
        {
            await _deviceService.UpdateDeviceAsync(id, dto);
            return Success();
        }

        /// <summary>
        /// 删除设备
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            await _deviceService.DeleteDeviceAsync(id);
            return Success();
        }

        /// <summary>
        /// 启动设备
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartDevice(int id)
        {
            await _deviceService.StartDeviceAsync(id);
            return Success();
        }

        /// <summary>
        /// 停止设备
        /// </summary>
        [HttpPost("{id}/stop")]
        public async Task<IActionResult> StopDevice(int id)
        {
            await _deviceService.StopDeviceAsync(id);
            return Success();
        }



        /// <summary>
        /// 更新设备启用状态
        /// </summary>
        [HttpPut("{id}/enable")]
        public async Task<IActionResult> UpdateDeviceEnable(int id, [FromBody] UpdateDeviceEnableDto dto)
        {
            await _deviceService.UpdateDeviceEnableAsync(id, dto.Enable);
            return Success();
        }
    }
} 