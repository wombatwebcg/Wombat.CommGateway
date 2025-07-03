using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Application.Services;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 数据采集控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DataCollectionController : ApiControllerBase
    {
        private readonly IDataCollectionService _collectionService;

        public DataCollectionController(IDataCollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        /// <summary>
        /// 启动数据采集
        /// </summary>
        [HttpPost("device/{deviceId}/start")]
        public async Task<IActionResult> StartCollection(int deviceId)
        {
            await _collectionService.StartCollectionAsync(deviceId);
            return Success();
        }

        /// <summary>
        /// 停止数据采集
        /// </summary>
        [HttpPost("device/{deviceId}/stop")]
        public async Task<IActionResult> StopCollection(int deviceId)
        {
            await _collectionService.StopCollectionAsync(deviceId);
            return Success();
        }

        /// <summary>
        /// 获取采集数据
        /// </summary>
        [HttpGet("device/{deviceId}/data")]
        public async Task<ActionResult<IEnumerable<DataCollectionRecord>>> GetCollectionData(int deviceId, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            var data = await _collectionService.GetCollectionDataAsync(deviceId, startTime, endTime);
            return Success(data);
        }

        /// <summary>
        /// 获取实时数据
        /// </summary>
        [HttpGet("device/{deviceId}/realtime")]
        public async Task<ActionResult<Dictionary<int, object>>> GetRealtimeData(int deviceId)
        {
            var data = await _collectionService.GetRealtimeDataAsync(deviceId);
            return Success(data);
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        [HttpPost("point/{pointId}/write")]
        public async Task<IActionResult> WriteData(int pointId, [FromBody] object value)
        {
            await _collectionService.WriteDataAsync(pointId, value);
            return Success();
        }

        /// <summary>
        /// 批量写入数据
        /// </summary>
        [HttpPost("batch-write")]
        public async Task<IActionResult> BatchWriteData([FromBody] Dictionary<int, object> pointValues)
        {
            await _collectionService.BatchWriteDataAsync(pointValues);
            return Success();
        }

        /// <summary>
        /// 获取采集状态
        /// </summary>
        [HttpGet("device/{deviceId}/status")]
        public async Task<ActionResult<bool>> GetCollectionStatus(int deviceId)
        {
            var status = await _collectionService.GetCollectionStatusAsync(deviceId);
            return Success(status);
        }

        /// <summary>
        /// 获取采集错误信息
        /// </summary>
        [HttpGet("device/{deviceId}/error")]
        public async Task<ActionResult<string>> GetCollectionError(int deviceId)
        {
            var error = await _collectionService.GetCollectionErrorAsync(deviceId);
            return Success(error);
        }
    }
} 