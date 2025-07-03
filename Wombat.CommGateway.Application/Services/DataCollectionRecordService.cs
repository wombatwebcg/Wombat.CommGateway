using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;
using Wombat.CommGateway.Infrastructure.Repositories;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 数据采集记录服务实现
    /// </summary>
    /// 
    [AutoInject(typeof(IDataCollectionRecordService))]
    public class DataCollectionRecordService : IDataCollectionRecordService
    {
        private readonly IDataCollectionRecordRepository _recordRepository;

        public DataCollectionRecordService(IDataCollectionRecordRepository recordRepository)
        {
            _recordRepository = recordRepository ?? throw new ArgumentNullException(nameof(recordRepository));
        }

        public async Task<(List<DataCollectionRecordDto> Records, int TotalCount)> GetListAsync(QueryDataCollectionRecordRequest request)
        {
            var query = await _recordRepository.GetAllAsync();
            var records = query.AsQueryable();

            if (request.DeviceId.HasValue)
            {
                records = records.Where(r => r.DeviceId == request.DeviceId.Value);
            }

            if (request.PointId.HasValue)
            {
                records = records.Where(r => r.PointId == request.PointId.Value);
            }

            if (request.StartTime.HasValue)
            {
                records = records.Where(r => r.Timestamp >= request.StartTime.Value);
            }

            if (request.EndTime.HasValue)
            {
                records = records.Where(r => r.Timestamp <= request.EndTime.Value);
            }

            if (request.Quality.HasValue)
            {
                records = records.Where(r => r.Quality == request.Quality.Value.ToString());
            }

            var totalCount = records.Count();
            var pagedRecords = records
                .OrderByDescending(r => r.Timestamp)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return (pagedRecords.ConvertAll(record => new DataCollectionRecordDto
            {
                Id = record.Id,
                DeviceId = record.DeviceId,
                PointId = record.PointId,
                Value = record.Value,
                Quality = Enum.Parse<DataQuality>(record.Quality),
                Timestamp = record.Timestamp
            }), totalCount);
        }

        public async Task<DataCollectionRecordDto> GetByIdAsync(int id)
        {
            var record = await _recordRepository.GetByIdAsync(id);
            if (record == null)
                return null;

            return new DataCollectionRecordDto
            {
                Id = record.Id,
                DeviceId = record.DeviceId,
                PointId = record.PointId,
                Value = record.Value,
                Quality = Enum.Parse<DataQuality>(record.Quality),
                Timestamp = record.Timestamp
            };
        }

        public async Task<DataCollectionRecordDto> CreateAsync(CreateDataCollectionRecordRequest request)
        {
            var record = new DataCollectionRecord(
                request.DeviceId, 
                request.PointId, 
                request.Value, 
                request.Quality.ToString());
            
            await _recordRepository.InsertAsync(record);

            return new DataCollectionRecordDto
            {
                Id = record.Id,
                DeviceId = record.DeviceId,
                PointId = record.PointId,
                Value = record.Value,
                Quality = request.Quality,
                Timestamp = record.Timestamp
            };
        }

        public async Task<List<DataCollectionRecordDto>> CreateBatchAsync(List<CreateDataCollectionRecordRequest> requests)
        {
            var records = requests.ConvertAll(request => 
                new DataCollectionRecord(
                    request.DeviceId, 
                    request.PointId, 
                    request.Value, 
                    request.Quality.ToString()));
            
            await _recordRepository.InsertAsync(records);

            var dtos = new List<DataCollectionRecordDto>();
            for (int i = 0; i < records.Count; i++)
            {
                dtos.Add(new DataCollectionRecordDto
                {
                    Id = records[i].Id,
                    DeviceId = records[i].DeviceId,
                    PointId = records[i].PointId,
                    Value = records[i].Value,
                    Quality = requests[i].Quality,
                    Timestamp = records[i].Timestamp
                });
            }
            return dtos;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var record = await _recordRepository.GetByIdAsync(id);
            if (record == null)
                return false;

            await _recordRepository.DeleteAsync(record);
            return true;
        }

        public async Task<bool> DeleteBatchAsync(List<int> ids)
        {
            var records = await _recordRepository.Select.Where(r => ids.Contains(r.Id)).ToListAsync();
            if (!records.Any())
                return false;

            await _recordRepository.DeleteAsync(records);
            return true;
        }

        public async Task<int> CleanupHistoryAsync(DateTime beforeDate)
        {
            var records = await _recordRepository.Select.Where(r => r.Timestamp < beforeDate).ToListAsync();
            if (!records.Any())
                return 0;

            await _recordRepository.DeleteAsync(records);
            return records.Count;
        }

        public async Task<IEnumerable<DataCollectionRecord>> GetCollectionDataAsync(int deviceId, DateTime startTime, DateTime endTime)
        {
            return await _recordRepository.Select
                .Where(r => r.DeviceId == deviceId && r.Timestamp >= startTime && r.Timestamp <= endTime)
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();
        }

        public async Task SaveCollectionDataAsync(DataCollectionRecord record)
        {
            await _recordRepository.InsertAsync(record);
        }

        public async Task BatchSaveCollectionDataAsync(IEnumerable<DataCollectionRecord> records)
        {
            await _recordRepository.InsertAsync(records);
        }
    }
} 