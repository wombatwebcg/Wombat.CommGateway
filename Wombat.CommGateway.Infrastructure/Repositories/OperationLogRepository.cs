using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;

namespace Wombat.CommGateway.Infrastructure.Repositories
{
    /// <summary>
    /// 操作日志仓储实现
    /// </summary>
    [AutoInject(typeof(IOperationLogRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class OperationLogRepository : BaseRepository<OperationLog, GatawayDB>, IOperationLogRepository
    {
        private readonly IServiceProvider _service;

        public OperationLogRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<List<OperationLog>> GetAllAsync()
        {
            return await Select
                .OrderByDescending(o => o.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<OperationLog> GetByIdAsync(int id)
        {
            return await Select
                .Where(o => o.Id == id)
                .FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<(List<OperationLog> items, int totalCount)> GetPagedAsync(
            int page, int pageSize,
            int? userId = null,
            string action = null,
            string resource = null,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            var query = Select;

            // 应用过滤条件
            if (userId.HasValue)
                query = query.Where(o => o.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(o => o.Action == action);

            if (!string.IsNullOrWhiteSpace(resource))
                query = query.Where(o => o.Resource == resource);

            if (startTime.HasValue)
                query = query.Where(o => o.Timestamp >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(o => o.Timestamp <= endTime.Value);

            // 获取总数
            var totalCount = (int)await query.CountAsync();

            // 分页查询
            var items = await query
                .OrderByDescending(o => o.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <inheritdoc/>
        public async Task<List<OperationLog>> GetByUserIdAsync(int userId)
        {
            return await Select
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<OperationLog>> GetByActionAsync(string action)
        {
            return await Select
                .Where(o => o.Action == action)
                .OrderByDescending(o => o.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<OperationLog>> GetByResourceAsync(string resource)
        {
            return await Select
                .Where(o => o.Resource == resource)
                .OrderByDescending(o => o.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<int> DeleteBeforeAsync(DateTime beforeTime)
        {
            var logsToDelete = await Select
                .Where(o => o.Timestamp < beforeTime)
                .ToListAsync();

            if (logsToDelete.Count > 0)
            {
                return await DeleteAsync(logsToDelete);
            }

            return 0;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, int>> GetActionStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            var query = Select;

            if (startTime.HasValue)
                query = query.Where(o => o.Timestamp >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(o => o.Timestamp <= endTime.Value);

            var logs = await query.ToListAsync();
            
            return logs.GroupBy(o => o.Action)
                      .ToDictionary(g => g.Key, g => g.Count());
        }
    }
} 