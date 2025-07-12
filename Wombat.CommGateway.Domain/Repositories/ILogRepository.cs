using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 系统日志仓储接口
    /// </summary>
    public interface ISystemLogRepository : IRepositoryKey<SystemLog>
    {
        /// <summary>
        /// 获取所有系统日志
        /// </summary>
        Task<List<SystemLog>> GetAllAsync();

        /// <summary>
        /// 根据ID获取系统日志
        /// </summary>
        Task<SystemLog> GetByIdAsync(int id);

        /// <summary>
        /// 根据条件分页查询系统日志
        /// </summary>
        Task<(List<SystemLog> items, int totalCount)> GetPagedAsync(
            int page, int pageSize, 
            LogLevel? level = null, 
            LogCategory? category = null, 
            DateTime? startTime = null, 
            DateTime? endTime = null,
            string keyword = null);

        /// <summary>
        /// 根据级别获取日志
        /// </summary>
        Task<List<SystemLog>> GetByLevelAsync(LogLevel level);

        /// <summary>
        /// 根据分类获取日志
        /// </summary>
        Task<List<SystemLog>> GetByCategoryAsync(LogCategory category);

        /// <summary>
        /// 根据时间范围获取日志
        /// </summary>
        Task<List<SystemLog>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// 删除指定时间之前的日志
        /// </summary>
        Task<int> DeleteBeforeAsync(DateTime beforeTime);

        /// <summary>
        /// 获取日志统计信息
        /// </summary>
        Task<Dictionary<LogLevel, int>> GetStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null);
    }

    /// <summary>
    /// 操作日志仓储接口
    /// </summary>
    public interface IOperationLogRepository : IRepositoryKey<OperationLog>
    {
        /// <summary>
        /// 获取所有操作日志
        /// </summary>
        Task<List<OperationLog>> GetAllAsync();

        /// <summary>
        /// 根据ID获取操作日志
        /// </summary>
        Task<OperationLog> GetByIdAsync(int id);

        /// <summary>
        /// 根据条件分页查询操作日志
        /// </summary>
        Task<(List<OperationLog> items, int totalCount)> GetPagedAsync(
            int page, int pageSize,
            int? userId = null,
            string action = null,
            string resource = null,
            DateTime? startTime = null,
            DateTime? endTime = null);

        /// <summary>
        /// 根据用户ID获取操作日志
        /// </summary>
        Task<List<OperationLog>> GetByUserIdAsync(int userId);

        /// <summary>
        /// 根据操作类型获取日志
        /// </summary>
        Task<List<OperationLog>> GetByActionAsync(string action);

        /// <summary>
        /// 根据资源类型获取日志
        /// </summary>
        Task<List<OperationLog>> GetByResourceAsync(string resource);

        /// <summary>
        /// 删除指定时间之前的日志
        /// </summary>
        Task<int> DeleteBeforeAsync(DateTime beforeTime);

        /// <summary>
        /// 获取操作统计信息
        /// </summary>
        Task<Dictionary<string, int>> GetActionStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null);
    }

    /// <summary>
    /// 通信日志仓储接口
    /// </summary>
    public interface ICommunicationLogRepository : IRepositoryKey<CommunicationLog>
    {
        /// <summary>
        /// 获取所有通信日志
        /// </summary>
        Task<List<CommunicationLog>> GetAllAsync();

        /// <summary>
        /// 根据ID获取通信日志
        /// </summary>
        Task<CommunicationLog> GetByIdAsync(int id);

        /// <summary>
        /// 根据条件分页查询通信日志
        /// </summary>
        Task<(List<CommunicationLog> items, int totalCount)> GetPagedAsync(
            int page, int pageSize,
            int? channelId = null,
            int? deviceId = null,
            string direction = null,
            string protocol = null,
            string status = null,
            DateTime? startTime = null,
            DateTime? endTime = null);

        /// <summary>
        /// 根据通道ID获取通信日志
        /// </summary>
        Task<List<CommunicationLog>> GetByChannelIdAsync(int channelId);

        /// <summary>
        /// 根据设备ID获取通信日志
        /// </summary>
        Task<List<CommunicationLog>> GetByDeviceIdAsync(int deviceId);

        /// <summary>
        /// 根据通信方向获取日志
        /// </summary>
        Task<List<CommunicationLog>> GetByDirectionAsync(string direction);

        /// <summary>
        /// 根据协议类型获取日志
        /// </summary>
        Task<List<CommunicationLog>> GetByProtocolAsync(string protocol);

        /// <summary>
        /// 根据状态获取日志
        /// </summary>
        Task<List<CommunicationLog>> GetByStatusAsync(string status);

        /// <summary>
        /// 删除指定时间之前的日志
        /// </summary>
        Task<int> DeleteBeforeAsync(DateTime beforeTime);

        /// <summary>
        /// 获取通信统计信息
        /// </summary>
        Task<Dictionary<string, int>> GetStatusStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// 获取平均响应时间
        /// </summary>
        Task<double> GetAverageResponseTimeAsync(DateTime? startTime = null, DateTime? endTime = null);
    }
} 