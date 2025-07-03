using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;


namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 数据采集记录仓储接口
    /// </summary>
    public interface IDataCollectionRecordRepository : IRepositoryKey<DataCollectionRecord>
    {
        /// <summary>
        /// 获取所有数据采集记录
        /// </summary>
        Task<List<DataCollectionRecord>> GetAllAsync();

        /// <summary>
        /// 根据ID获取数据采集记录
        /// </summary>
        Task<DataCollectionRecord> GetByIdAsync(int id);



        /// <summary>
        /// 删除数据采集记录
        /// </summary>
         Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 根据设备ID获取数据采集记录
        /// </summary>
        Task<List<DataCollectionRecord>> GetByDeviceIdAsync(int deviceId);

        /// <summary>
        /// 根据点位ID获取数据采集记录
        /// </summary>
        Task<List<DataCollectionRecord>> GetByPointIdAsync(int pointId);

        /// <summary>
        /// 根据时间范围获取数据采集记录
        /// </summary>
        Task<List<DataCollectionRecord>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// 根据数据质量获取数据采集记录
        /// </summary>
        Task<List<DataCollectionRecord>> GetByQualityAsync(DataQuality quality);

        /// <summary>
        /// 删除指定时间之前的数据采集记录
        /// </summary>
        Task<int> DeleteByTimeRangeAsync(DateTime beforeTime);
    }
} 