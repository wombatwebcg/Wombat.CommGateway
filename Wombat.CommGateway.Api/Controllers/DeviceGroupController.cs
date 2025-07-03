using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;

namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 设备组控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceGroupController : ApiControllerBase
    {
        private readonly IDeviceGroupService _deviceGroupService;

        public DeviceGroupController(IDeviceGroupService deviceGroupService)
        {
            _deviceGroupService = deviceGroupService;
        }

        /// <summary>
        /// 获取所有设备组
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<DeviceGroupDto>>> GetAllDeviceGroups()
        {
            var deviceGroups = await _deviceGroupService.GetAllDeviceGroupsAsync();
            return Success(deviceGroups);
        }

        /// <summary>
        /// 根据ID获取设备组
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DeviceGroupDto>> GetDeviceGroupById(int id)
        {
            var deviceGroup = await _deviceGroupService.GetDeviceGroupByIdAsync(id);
            if (deviceGroup == null)
                return NotFound();
            return Success(deviceGroup);
        }

        /// <summary>
        /// 创建设备组
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DeviceGroupDto>> CreateDeviceGroup([FromBody] CreateDeviceGroupDto dto)
        {
            var deviceGroup = await _deviceGroupService.CreateDeviceGroupAsync(dto);
            return Success(deviceGroup);
        }

        /// <summary>
        /// 更新设备组
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<DeviceGroupDto>> UpdateDeviceGroup(int id, [FromBody] UpdateDeviceGroupDto dto)
        {
            var deviceGroup = await _deviceGroupService.UpdateDeviceGroupAsync(id, dto);
            return Success(deviceGroup);
        }

        /// <summary>
        /// 删除设备组
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeviceGroup(int id)
        {
            var result = await _deviceGroupService.DeleteDeviceGroupAsync(id);
            if (!result)
                return NotFound();
            return Success();
        }
    }
} 