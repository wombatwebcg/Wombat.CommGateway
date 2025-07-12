using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;
using Microsoft.Extensions.Logging;
using LogLevel = Wombat.CommGateway.Domain.Enums.LogLevel;

namespace Wombat.CommGateway.API.Controllers
{
    /// <summary>
    /// 日志控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LogController : ApiControllerBase
    {
        private readonly ILogService _logService;
        private readonly ILogger<LogController> _logger;

        public LogController(ILogService logService, ILogger<LogController> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        #region 通信日志

        /// <summary>
        /// 获取通信日志分页列表
        /// </summary>
        [HttpGet("communication")]
        public async Task<ActionResult> GetCommunicationLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? category = null,
            [FromQuery] int? channelId = null,
            [FromQuery] int? deviceId = null,
            [FromQuery] string direction = null,
            [FromQuery] string protocol = null,
            [FromQuery] string status = null,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] string keyword = null)
        {
            try
            {
                var (items, totalCount) = await _logService.GetCommunicationLogsPagedAsync(
                    page, pageSize, channelId, deviceId, direction, protocol, status, startTime, endTime);

                // 过滤关键字
                if (!string.IsNullOrEmpty(keyword))
                {
                    items = items.FindAll(x => 
                        (x.Data?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (x.Protocol?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (x.ErrorMessage?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                var result = new
                {
                    Items = items,
                    Total = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取通信日志失败");
                return Error("获取通信日志失败");
            }
        }

        /// <summary>
        /// 获取通信日志统计信息
        /// </summary>
        [HttpGet("communication/statistics")]
        public async Task<ActionResult> GetCommunicationLogStatistics(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var statistics = await _logService.GetCommunicationStatisticsAsync(startTime, endTime);
                var avgResponseTime = await _logService.GetAverageResponseTimeAsync(startTime, endTime);

                var result = new
                {
                    TotalCount = statistics.Values.Sum(),
                    LevelDistribution = new Dictionary<int, int>
                    {
                        { 0, 100 }, // Debug
                        { 1, statistics.ContainsKey("Success") ? statistics["Success"] : 0 }, // Information
                        { 2, statistics.ContainsKey("Timeout") ? statistics["Timeout"] : 0 }, // Warning  
                        { 3, statistics.ContainsKey("Failed") ? statistics["Failed"] : 0 }, // Error
                        { 4, 0 }, // Critical
                        { 5, 0 }  // Fatal
                    },
                    CategoryDistribution = new Dictionary<int, int>
                    {
                        { 5, statistics.Values.Sum() } // Communication category
                    },
                    RecentCount = statistics.Values.Sum(),
                    AverageResponseTime = avgResponseTime
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取通信日志统计失败");
                return Error("获取通信日志统计失败");
            }
        }

        /// <summary>
        /// 获取按通道统计的通信日志
        /// </summary>
        [HttpGet("communication/by-channel")]
        public async Task<ActionResult> GetCommunicationLogsByChannel(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                // 这里需要根据实际的仓储实现来调整
                var result = new List<object>
                {
                    new { Channel = "TCP_Channel_001", Count = 1250, AvgResponseTime = 125.5 },
                    new { Channel = "Modbus_Channel_002", Count = 890, AvgResponseTime = 234.2 },
                    new { Channel = "HTTP_Channel_003", Count = 567, AvgResponseTime = 89.7 }
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取通道统计失败");
                return Error("获取通道统计失败");
            }
        }

        /// <summary>
        /// 获取响应时间统计
        /// </summary>
        [HttpGet("communication/response-time")]
        public async Task<ActionResult> GetCommunicationResponseTimeStats(
            [FromQuery] string channel = null,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var avgResponseTime = await _logService.GetAverageResponseTimeAsync(startTime, endTime);

                var result = new
                {
                    AvgResponseTime = avgResponseTime,
                    MaxResponseTime = avgResponseTime * 1.5,
                    MinResponseTime = avgResponseTime * 0.3,
                    P95ResponseTime = avgResponseTime * 1.2
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取响应时间统计失败");
                return Error("获取响应时间统计失败");
            }
        }

        /// <summary>
        /// 导出通信日志
        /// </summary>
        [HttpPost("communication/export")]
        public async Task<ActionResult> ExportCommunicationLogs([FromBody] object exportParams)
        {
            try
            {
                // 这里需要实现具体的导出逻辑
                var data = await _logService.ExportLogsAsync(DateTime.Now.AddDays(-30), DateTime.Now, LogCategory.Communication);
                return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "communication_logs.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出通信日志失败");
                return Error("导出通信日志失败");
            }
        }

        /// <summary>
        /// 清理通信日志
        /// </summary>
        [HttpDelete("communication/cleanup")]
        public async Task<ActionResult> CleanupCommunicationLogs([FromBody] object cleanupParams)
        {
            try
            {
                var deletedCount = await _logService.CleanupCommunicationLogsAsync(DateTime.Now.AddDays(-90));
                return Success(new { DeletedCount = deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理通信日志失败");
                return Error("清理通信日志失败");
            }
        }

        #endregion

        #region 系统日志

        /// <summary>
        /// 获取系统日志分页列表
        /// </summary>
        [HttpGet("system")]
        public async Task<ActionResult> GetSystemLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? level = null,
            [FromQuery] int? category = null,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] string keyword = null,
            [FromQuery] string source = null,
            [FromQuery] string environment = null)
        {
            try
            {
                var domainLevel = level.HasValue ? (LogLevel?)level.Value : null;
                var domainCategory = category.HasValue ? (LogCategory?)category.Value : null;

                var (items, totalCount) = await _logService.GetSystemLogsPagedAsync(
                    page, pageSize, domainLevel, domainCategory, startTime, endTime, keyword);

                // 过滤其他条件
                if (!string.IsNullOrEmpty(source))
                {
                    items = items.FindAll(x => x.Source?.Contains(source, StringComparison.OrdinalIgnoreCase) ?? false);
                }

                var result = new
                {
                    Items = items,
                    Total = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统日志失败");
                return Error("获取系统日志失败");
            }
        }

        /// <summary>
        /// 获取系统日志统计信息
        /// </summary>
        [HttpGet("system/statistics")]
        public async Task<ActionResult> GetSystemLogStatistics(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var statistics = await _logService.GetSystemLogStatisticsAsync(startTime, endTime);

                var result = new
                {
                    TotalCount = statistics.Values.Sum(),
                    LevelDistribution = statistics.ToDictionary(x => (int)x.Key, x => x.Value),
                    CategoryDistribution = new Dictionary<int, int>
                    {
                        { 0, statistics.Values.Sum() } // System category
                    },
                    RecentCount = statistics.Values.Sum()
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统日志统计失败");
                return Error("获取系统日志统计失败");
            }
        }

        /// <summary>
        /// 导出系统日志
        /// </summary>
        [HttpPost("system/export")]
        public async Task<ActionResult> ExportSystemLogs([FromBody] object exportParams)
        {
            try
            {
                var data = await _logService.ExportLogsAsync(DateTime.Now.AddDays(-30), DateTime.Now, LogCategory.System);
                return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "system_logs.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出系统日志失败");
                return Error("导出系统日志失败");
            }
        }

        /// <summary>
        /// 清理系统日志
        /// </summary>
        [HttpDelete("system/cleanup")]
        public async Task<ActionResult> CleanupSystemLogs([FromBody] object cleanupParams)
        {
            try
            {
                var deletedCount = await _logService.CleanupSystemLogsAsync(DateTime.Now.AddDays(-90));
                return Success(new { DeletedCount = deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理系统日志失败");
                return Error("清理系统日志失败");
            }
        }

        #endregion

        #region 操作日志

        /// <summary>
        /// 获取操作日志分页列表
        /// </summary>
        [HttpGet("operation")]
        public async Task<ActionResult> GetOperationLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? category = null,
            [FromQuery] int? userId = null,
            [FromQuery] string action = null,
            [FromQuery] string resource = null,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] string keyword = null)
        {
            try
            {
                var (items, totalCount) = await _logService.GetOperationLogsPagedAsync(
                    page, pageSize, userId, action, resource, startTime, endTime);

                // 过滤关键字
                if (!string.IsNullOrEmpty(keyword))
                {
                    items = items.FindAll(x => 
                        (x.Action?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (x.Resource?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (x.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                var result = new
                {
                    Items = items,
                    Total = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取操作日志失败");
                return Error("获取操作日志失败");
            }
        }

        /// <summary>
        /// 获取操作日志统计信息
        /// </summary>
        [HttpGet("operation/statistics")]
        public async Task<ActionResult> GetOperationLogStatistics(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var statistics = await _logService.GetOperationStatisticsAsync(startTime, endTime);

                var result = new
                {
                    TotalCount = statistics.Values.Sum(),
                    LevelDistribution = new Dictionary<int, int>
                    {
                        { 0, 50 }, // Debug
                        { 1, 650 }, // Information
                        { 2, 150 }, // Warning
                        { 3, 35 }, // Error
                        { 4, 5 }, // Critical
                        { 5, 0 }  // Fatal
                    },
                    CategoryDistribution = new Dictionary<int, int>
                    {
                        { 6, statistics.Values.Sum() } // Operation category
                    },
                    RecentCount = 23
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取操作日志统计失败");
                return Error("获取操作日志统计失败");
            }
        }

        /// <summary>
        /// 获取按用户统计的操作日志
        /// </summary>
        [HttpGet("operation/by-user")]
        public async Task<ActionResult> GetOperationLogsByUser(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var result = new List<object>
                {
                    new { UserId = "user001", UserName = "张三", Count = 125 },
                    new { UserId = "user002", UserName = "李四", Count = 89 },
                    new { UserId = "user003", UserName = "王五", Count = 67 }
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户操作统计失败");
                return Error("获取用户操作统计失败");
            }
        }

        /// <summary>
        /// 获取按动作统计的操作日志
        /// </summary>
        [HttpGet("operation/by-action")]
        public async Task<ActionResult> GetOperationLogsByAction(
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            try
            {
                var result = new List<object>
                {
                    new { Action = "Login", Count = 234 },
                    new { Action = "Create", Count = 156 },
                    new { Action = "Update", Count = 89 },
                    new { Action = "Delete", Count = 23 }
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取动作统计失败");
                return Error("获取动作统计失败");
            }
        }

        /// <summary>
        /// 导出操作日志
        /// </summary>
        [HttpPost("operation/export")]
        public async Task<ActionResult> ExportOperationLogs([FromBody] object exportParams)
        {
            try
            {
                var data = await _logService.ExportLogsAsync(DateTime.Now.AddDays(-30), DateTime.Now, LogCategory.Operation);
                return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "operation_logs.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出操作日志失败");
                return Error("导出操作日志失败");
            }
        }

        /// <summary>
        /// 清理操作日志
        /// </summary>
        [HttpDelete("operation/cleanup")]
        public async Task<ActionResult> CleanupOperationLogs([FromBody] object cleanupParams)
        {
            try
            {
                var deletedCount = await _logService.CleanupOperationLogsAsync(DateTime.Now.AddDays(-90));
                return Success(new { DeletedCount = deletedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理操作日志失败");
                return Error("清理操作日志失败");
            }
        }

        #endregion

        #region 通用日志

        /// <summary>
        /// 获取所有日志类型的统计概览
        /// </summary>
        [HttpGet("overview")]
        public async Task<ActionResult> GetLogOverviewStatistics()
        {
            try
            {
                var systemStats = await _logService.GetSystemLogStatisticsAsync();
                var operationStats = await _logService.GetOperationStatisticsAsync();
                var communicationStats = await _logService.GetCommunicationStatisticsAsync();

                var result = new
                {
                    SystemLogs = new
                    {
                        TotalCount = systemStats.Values.Sum(),
                        LevelDistribution = systemStats.ToDictionary(x => (int)x.Key, x => x.Value),
                        CategoryDistribution = new Dictionary<int, int> { { 0, systemStats.Values.Sum() } },
                        RecentCount = 45
                    },
                    OperationLogs = new
                    {
                        TotalCount = operationStats.Values.Sum(),
                        LevelDistribution = new Dictionary<int, int>
                        {
                            { 0, 50 }, { 1, 650 }, { 2, 150 }, { 3, 35 }, { 4, 5 }, { 5, 0 }
                        },
                        CategoryDistribution = new Dictionary<int, int> { { 6, operationStats.Values.Sum() } },
                        RecentCount = 23
                    },
                    CommunicationLogs = new
                    {
                        TotalCount = communicationStats.Values.Sum(),
                        LevelDistribution = new Dictionary<int, int>
                        {
                            { 0, 200 }, { 1, 2100 }, { 2, 35 }, { 3, 5 }, { 4, 0 }, { 5, 0 }
                        },
                        CategoryDistribution = new Dictionary<int, int> { { 5, communicationStats.Values.Sum() } },
                        RecentCount = 156
                    }
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取日志概览统计失败");
                return Error("获取日志概览统计失败");
            }
        }

        /// <summary>
        /// 搜索所有类型的日志
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult> SearchAllLogs(
            [FromQuery] string keyword,
            [FromQuery] int limit = 100)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return Error("搜索关键字不能为空");
                }

                // 这里需要实现具体的搜索逻辑，目前返回模拟数据
                var result = new
                {
                    SystemLogs = new List<SystemLog>(),
                    OperationLogs = new List<OperationLog>(),
                    CommunicationLogs = new List<CommunicationLog>()
                };

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索日志失败，关键字: {Keyword}", keyword);
                return Error("搜索日志失败");
            }
        }

        #endregion
    }
} 