using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;
using Wombat.CommGateway.Domain.Enums;

namespace Wombat.CommGateway.Infrastructure.Repositories
{
    /// <summary>
    /// 系统日志仓储实现
    /// </summary>
    [AutoInject(typeof(ISystemLogRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class SystemLogRepository : BaseRepository<SystemLog, GatawayDB>, ISystemLogRepository
    {
        private readonly IServiceProvider _service;

        public SystemLogRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<List<SystemLog>> GetAllAsync()
        {
            return await Select
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<SystemLog> GetByIdAsync(int id)
        {
            return await Select
                .Where(s => s.Id == id)
                .FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<(List<SystemLog> items, int totalCount)> GetPagedAsync(
            int page, int pageSize,
            LogLevel? level = null,
            LogCategory? category = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            string keyword = null)
        {
            var query = Select;

            // 应用过滤条件
            if (level.HasValue)
                query = query.Where(s => s.Level == level.Value);

            if (category.HasValue)
                query = query.Where(s => s.Category == category.Value);

            if (startTime.HasValue)
                query = query.Where(s => s.Timestamp >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(s => s.Timestamp <= endTime.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(s => s.Message.Contains(keyword));
            }

            // 获取总数
            var totalCount = (int)await query.CountAsync();

            // 分页查询
            var items = await query
                .OrderByDescending(s => s.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <inheritdoc/>
        public async Task<List<SystemLog>> GetByLevelAsync(LogLevel level)
        {
            return await Select
                .Where(s => s.Level == level)
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<SystemLog>> GetByCategoryAsync(LogCategory category)
        {
            return await Select
                .Where(s => s.Category == category)
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<SystemLog>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            return await Select
                .Where(s => s.Timestamp >= startTime && s.Timestamp <= endTime)
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<int> DeleteBeforeAsync(DateTime beforeTime)
        {
            var logsToDelete = await Select
                .Where(s => s.Timestamp < beforeTime)
                .ToListAsync();

            if (logsToDelete.Count > 0)
            {
                return await DeleteAsync(logsToDelete);
            }

            return 0;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<LogLevel, int>> GetStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            var query = Select;

            if (startTime.HasValue)
                query = query.Where(s => s.Timestamp >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(s => s.Timestamp <= endTime.Value);

            var logs = await query.ToListAsync();
            
            return logs.GroupBy(s => s.Level)
                      .ToDictionary(g => g.Key, g => g.Count());
        }
    }
} 