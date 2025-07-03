using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 数据采集记录服务接口
    /// </summary>
    public interface IDataCollectionRecordService
    {
        /// <summary>
        /// 获取数据采集记录列表
        /// </summary>
        /// <param name="request">查询请求</param>
        /// <returns>数据采集记录列表</returns>
        Task<(List<DataCollectionRecordDto> Records, int TotalCount)> GetListAsync(QueryDataCollectionRecordRequest request);

        /// <summary>
        /// 获取数据采集记录详情
        /// </summary>
        /// <param name="id">记录ID</param>
        /// <returns>数据采集记录详情</returns>
        Task<DataCollectionRecordDto> GetByIdAsync(int id);

        /// <summary>
        /// 创建数据采集记录
        /// </summary>
        /// <param name="request">创建请求</param>
        /// <returns>创建的数据采集记录</returns>
        Task<DataCollectionRecordDto> CreateAsync(CreateDataCollectionRecordRequest request);

        /// <summary>
        /// 批量创建数据采集记录
        /// </summary>
        /// <param name="requests">创建请求列表</param>
        /// <returns>创建的数据采集记录列表</returns>
        Task<List<DataCollectionRecordDto>> CreateBatchAsync(List<CreateDataCollectionRecordRequest> requests);

        /// <summary>
        /// 删除数据采集记录
        /// </summary>
        /// <param name="id">记录ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 批量删除数据采集记录
        /// </summary>
        /// <param name="ids">记录ID列表</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteBatchAsync(List<int> ids);

        /// <summary>
        /// 清理历史数据
        /// </summary>
        /// <param name="beforeDate">清理此日期之前的数据</param>
        /// <returns>清理的记录数量</returns>
        Task<int> CleanupHistoryAsync(System.DateTime beforeDate);

        /// <summary>
        /// 获取采集数据
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>采集数据记录列表</returns>
        Task<IEnumerable<DataCollectionRecord>> GetCollectionDataAsync(int deviceId, DateTime startTime, DateTime endTime);

        /// <summary>
        /// 保存采集数据
        /// </summary>
        /// <param name="record">采集数据记录</param>
        Task SaveCollectionDataAsync(DataCollectionRecord record);

        /// <summary>
        /// 批量保存采集数据
        /// </summary>
        /// <param name="records">采集数据记录列表</param>
        Task BatchSaveCollectionDataAsync(IEnumerable<DataCollectionRecord> records);
    }
} 