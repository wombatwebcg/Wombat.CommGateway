using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 数据采集服务接口
    /// </summary>
    public interface IDataCollectionService
    {
        /// <summary>
        /// 启动数据采集
        /// </summary>
        Task StartCollectionAsync(int deviceId);

        /// <summary>
        /// 停止数据采集
        /// </summary>
        Task StopCollectionAsync(int deviceId);

        /// <summary>
        /// 获取采集数据
        /// </summary>
        Task<IEnumerable<DataCollectionRecord>> GetCollectionDataAsync(int deviceId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// 获取实时数据
        /// </summary>
        Task<Dictionary<int, object>> GetRealtimeDataAsync(int deviceId);

        /// <summary>
        /// 写入数据
        /// </summary>
        Task WriteDataAsync(int pointId, object value);

        /// <summary>
        /// 批量写入数据
        /// </summary>
        Task BatchWriteDataAsync(Dictionary<int, object> pointValues);

        /// <summary>
        /// 获取采集状态
        /// </summary>
        Task<bool> GetCollectionStatusAsync(int deviceId);

        /// <summary>
        /// 获取采集错误信息
        /// </summary>
        Task<string> GetCollectionErrorAsync(int deviceId);
    }
} 