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
    /// 通信日志仓储实现
    /// </summary>
    [AutoInject(typeof(ICommunicationLogRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class CommunicationLogRepository : BaseRepository<CommunicationLog, GatawayDB>, ICommunicationLogRepository
    {
        private readonly IServiceProvider _service;

        public CommunicationLogRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<List<CommunicationLog>> GetAllAsync()
        {
            return await Select
                .Include(c => c.Channel)
                .Include(c => c.Device)
                .OrderByDescending(c => c.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<CommunicationLog> GetByIdAsync(int id)
        {
            return await Select
                .Include(c => c.Channel)
                .Include(c => c.Device)
                .Where(c => c.Id == id)
                .FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<(List<CommunicationLog> items, int totalCount)> GetPagedAsync(
            int page, int pageSize,
            int? channelId = null,
            int? deviceId = null,
            string direction = null,
            string protocol = null,
            string status = null,
            DateTime? startTime = null,
            DateTime? endTime = null)
        {
            var query = Select;

            // 应用过滤条件
            if (channelId.HasValue)
                query = query.Where(c => c.ChannelId == channelId.Value);

            if (deviceId.HasValue)
                query = query.Where(c => c.DeviceId == deviceId.Value);

            if (!string.IsNullOrWhiteSpace(direction))
                query = query.Where(c => c.Direction == direction);

            if (!string.IsNullOrWhiteSpace(protocol))
                query = query.Where(c => c.Protocol == protocol);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            if (startTime.HasValue)
                query = query.Where(c => c.Timestamp >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(c => c.Timestamp <= endTime.Value);

            // 获取总数
            var totalCount = (int)await query.CountAsync();

            // 分页查询
            var items = await query
                .Include(c => c.Channel)
                .Include(c => c.Device)
                .OrderByDescending(c => c.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <inheritdoc/>
        public async Task<List<CommunicationLog>> GetByChannelIdAsync(int channelId)
        {
            return await Select
                .Include(c => c.Channel)
                .Include(c => c.Device)
                .Where(c => c.ChannelId == channelId)
                .OrderByDescending(c => c.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<CommunicationLog>> GetByDeviceIdAsync(int deviceId)
        {
            return await Select
                .Include(c => c.Channel)
                .Include(c => c.Device)
                .Where(c => c.DeviceId == deviceId)
                .OrderByDescending(c => c.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<CommunicationLog>> GetByDirectionAsync(string direction)
        {
            return await Select
                .Include(c => c.Channel)
                .Include(c => c.Device)
                .Where(c => c.Direction == direction)
                .OrderByDescending(c => c.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<CommunicationLog>> GetByProtocolAsync(string protocol)
        {
            return await Select
                .Include(c => c.Channel)
                .Include(c => c.Device)
                .Where(c => c.Protocol == protocol)
                .OrderByDescending(c => c.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<CommunicationLog>> GetByStatusAsync(string status)
        {
            return await Select
                .Include(c => c.Channel)
                .Include(c => c.Device)
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<int> DeleteBeforeAsync(DateTime beforeTime)
        {
            var logsToDelete = await Select
                .Where(c => c.Timestamp < beforeTime)
                .ToListAsync();

            if (logsToDelete.Count > 0)
            {
                return await DeleteAsync(logsToDelete);
            }

            return 0;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, int>> GetStatusStatisticsAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            var query = Select;

            if (startTime.HasValue)
                query = query.Where(c => c.Timestamp >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(c => c.Timestamp <= endTime.Value);

            var logs = await query.ToListAsync();
            
            return logs.GroupBy(c => c.Status)
                      .ToDictionary(g => g.Key ?? "Unknown", g => g.Count());
        }

        /// <inheritdoc/>
        public async Task<double> GetAverageResponseTimeAsync(DateTime? startTime = null, DateTime? endTime = null)
        {
            var query = Select;

            if (startTime.HasValue)
                query = query.Where(c => c.Timestamp >= startTime.Value);

            if (endTime.HasValue)
                query = query.Where(c => c.Timestamp <= endTime.Value);

            var logs = await query
                .Where(c => c.ResponseTime.HasValue)
                .ToListAsync();

            if (logs.Count == 0)
                return 0;

            return logs.Average(c => c.ResponseTime.Value);
        }
    }
} 