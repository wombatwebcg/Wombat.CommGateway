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
        /// 创建设备点位
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DevicePoint>> CreatePoint([FromBody] CreatePointRequest request)
        {
            var point = await _pointService.CreatePointAsync(new CreateDevicePointDto(){DeviceId = request.DeviceId, Name = request.Name, Address = request.Address, DataType = request.DataType, ScanRate = request.ScanRate});
            return Success(point);
        }

        /// <summary>
        /// 更新点位状态
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdatePointStatus(int id, [FromBody] UpdatePointStatusRequest request)
        {
            await _pointService.UpdatePointStatusAsync(id, request.IsEnabled);
            return Success();
        }

        /// <summary>
        /// 获取设备的所有点位
        /// </summary>
        [HttpGet("device/{deviceId}")]
        public async Task<ActionResult<IEnumerable<DevicePoint>>> GetDevicePoints(int deviceId)
        {
            var points = await _pointService.GetDevicePointsAsync(deviceId);
            return Success(points);
        }

        /// <summary>
        /// 获取点位详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DevicePoint>> GetPoint(int id)
        {
            var point = await _pointService.GetPointByIdAsync(id);
            if (point == null)
                return NotFound();
            return Success(point);
        }

        /// <summary>
        /// 更新点位配置
        /// </summary>
        [HttpPut("{id}/configuration")]
        public async Task<IActionResult> UpdatePointConfiguration(int id, [FromBody] UpdatePointConfigurationRequest request)
        {
            await _pointService.UpdatePointConfigurationAsync(id, new UpdateDevicePointDto(){Address = request.Address, DataType = request.DataType, ScanRate = request.ScanRate});
            return Success();
        }

        /// <summary>
        /// 删除点位
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoint(int id)
        {
            await _pointService.DeletePointAsync(id);
            return Success();
        }

        /// <summary>
        /// 批量导入点位
        /// </summary>
        [HttpPost("device/{deviceId}/import")]
        public async Task<IActionResult> ImportPoints(int deviceId, [FromBody] IEnumerable<DevicePoint> points)
        {
            await _pointService.ImportPointsAsync(deviceId, points);
            return Success();
        }

        /// <summary>
        /// 批量导出点位
        /// </summary>
        [HttpGet("device/{deviceId}/export")]
        public async Task<ActionResult<IEnumerable<DevicePoint>>> ExportPoints(int deviceId)
        {
            var points = await _pointService.ExportPointsAsync(deviceId);
            return Success(points);
        }
    }

    public class CreatePointRequest
    {
        public int DeviceId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string DataType { get; set; }
        public int ScanRate { get; set; }
    }

    public class UpdatePointStatusRequest
    {
        public bool IsEnabled { get; set; }
    }

    public class UpdatePointConfigurationRequest
    {
        public string Address { get; set; }
        public string DataType { get; set; }
        public int ScanRate { get; set; }
    }
} 