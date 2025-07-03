// Licensed to the Wombat.CommGateway.API under one or more agreements.
// The Wombat.CommGateway.API licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Services;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;

namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 设备点位控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DevicePointController : ApiControllerBase
    {
        private readonly IDevicePointService _pointService;

        public DevicePointController(IDevicePointService pointService)
        {
            _pointService = pointService;
        }

        /// <summary>
        /// 获取点位列表（分页）
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PointListResponseDto>> GetPoints([FromQuery] PointQueryDto query)
        {
            var result = await _pointService.GetPointsAsync(query);
            return Success(result);
        }

        /// <summary>
        /// 获取所有点位（不分页）
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<List<DevicePointDto>>> GetAllPoints()
        {
            var points = await _pointService.GetAllPointsAsync();
            return Success(points);
        }

        /// <summary>
        /// 获取点位详情
        /// </summary>
        [HttpGet("detail/{id}")]
        public async Task<ActionResult<DevicePointDto>> GetPointById(int id)
        {
            var point = await _pointService.GetPointByIdAsync(id);
            if (point == null)
                return NotFound();
            return Success(point);
        }

        /// <summary>
        /// 创建设备点位
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<int>> CreatePoint([FromBody] CreateDevicePointDto dto)
        {
            var id = await _pointService.CreatePointAsync(dto);
            return Success(id);
        }

        /// <summary>
        /// 更新设备点位
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePoint(int id, [FromBody] DevicePointDto dto)
        {
            await _pointService.UpdatePointAsync(id, dto);
            return Success();
        }

        /// <summary>
        /// 删除设备点位
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoint(int id)
        {
            await _pointService.DeletePointAsync(id);
            return Success();
        }

        /// <summary>
        /// 更新点位启用状态
        /// </summary>
        [HttpPut("{id}/enable")]
        public async Task<IActionResult> UpdatePointEnable(int id, [FromBody] UpdateDevicePointEnableDto dto)
        {
            await _pointService.UpdatePointEnableAsync(id, dto.Enable);
            return Success();
        }



        /// <summary>
        /// 获取设备下的所有点位
        /// </summary>
        [HttpGet("{deviceId}")]
        public async Task<ActionResult<List<DevicePointDto>>> GetDevicePoints(int deviceId)
        {
            var points = await _pointService.GetDevicePointsAsync(deviceId);
            return Success(points);
        }
    }
} 