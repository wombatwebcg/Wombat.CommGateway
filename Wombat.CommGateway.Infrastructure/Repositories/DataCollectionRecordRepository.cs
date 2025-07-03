using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;

namespace Wombat.CommGateway.Infrastructure.Repositories
{
    /// <summary>
    /// 数据采集记录仓储实现
    /// </summary>
    [AutoInject(typeof(IDataCollectionRecordRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class DataCollectionRecordRepository : BaseRepository<DataCollectionRecord, GatawayDB>, IDataCollectionRecordRepository
    {
        private readonly IServiceProvider _service;

        public DataCollectionRecordRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<List<DataCollectionRecord>> GetAllAsync()
        {
            return await Select.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<DataCollectionRecord> GetByIdAsync(int id)
        {
            return await Select.Where(r => r.Id == id).FirstAsync();
        }



        /// <inheritdoc/>
        public async Task<List<DataCollectionRecord>> GetByDeviceIdAsync(int deviceId)
        {
            return await Select.Where(r => r.DeviceId == deviceId).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<DataCollectionRecord>> GetByPointIdAsync(int pointId)
        {
            return await Select.Where(r => r.PointId == pointId).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<DataCollectionRecord>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            return await Select
                .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<DataCollectionRecord>> GetByQualityAsync(DataQuality quality)
        {
            return await Select.Where(r => r.Quality == quality.ToString()).ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<int> DeleteByTimeRangeAsync(DateTime beforeTime)
        {
            return await DeleteAsync(r => r.Timestamp < beforeTime);
        }


        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(int id)
        {
            var device = await GetByIdAsync(id);
            if (device != null)
            {
                var count = await base.DeleteAsync(device);
                if (count > 0) return true;
            }
            return false;
        }
    }
}