using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;
using Wombat.CommGateway.Infrastructure.Communication;
using Wombat.CommGateway.Infrastructure.Repositories;
using Wombat.Infrastructure;

namespace Wombat.CommGateway.Application.Services
{
    /// <summary>
    /// 通信通道服务实现
    /// </summary>
    [AutoInject(typeof(IChannelService))]
    public class ChannelService : IChannelService
    {
        private readonly IChannelRepository _channelRepository;
        private readonly IProtocolConfigRepository _protocolConfigRepository;
        private readonly IProtocolFactory _protocolFactory;
        private readonly Dictionary<int, IProtocol> _protocolInstances;

        public ChannelService(
            IChannelRepository channelRepository,
            IProtocolConfigRepository protocolConfigRepository,
            IProtocolFactory protocolFactory)
        {
            _channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
            _protocolConfigRepository = protocolConfigRepository ?? throw new ArgumentNullException(nameof(protocolConfigRepository));
            _protocolFactory = protocolFactory ?? throw new ArgumentNullException(nameof(protocolFactory));
            _protocolInstances = new Dictionary<int, IProtocol>();
        }

        public async Task<List<ChannelDto>> GetListAsync()
        {
            var channels = await _channelRepository.GetAllAsync();
            return channels.ConvertAll(channel => new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Type = (int)channel.Type,
                Protocol = (int)channel.Protocol,
                Role = (int)channel.Role,
                Status = (int)channel.Status,
                Enable = channel.Enable,
                CreateTime = channel.CreateTime,
                Configuration = channel.Configuration
            });
        }

        public async Task<ChannelDto> GetByIdAsync(int id)
        {
            var channel = await _channelRepository.GetByIdAsync(id);
            if (channel == null)
                return null;

            return new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Type = (int)channel.Type,
                Protocol = (int)channel.Protocol,
                Role = (int)channel.Role,
                Status = (int)channel.Status,
                Enable = channel.Enable,
                CreateTime = channel.CreateTime,
                Configuration = channel.Configuration
            };
        }

        public async Task<ChannelDto> CreateAsync(CreateChannelDto dto)
        {
            var channel = new Channel(dto.Name, (ChannelType)dto.Type, (ProtocolType)dto.Protocol, (ChannelRole)dto.Role, 0, dto.Enable);
            if (dto.Configuration != null)
            {
                channel.UpdateConfiguration(dto.Configuration);
            }

            await _channelRepository.InsertAsync(channel);

            return new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Type = (int)channel.Type,
                Protocol = (int)channel.Protocol,
                Role = (int)channel.Role,
                Status = (int)channel.Status,
                Enable = channel.Enable,
                CreateTime = channel.CreateTime,
                Configuration = channel.Configuration
            };
        }

        public async Task<ChannelDto> UpdateConfigurationAsync(int id, Dictionary<string, string> configuration)
        {
            var channel = await _channelRepository.GetByIdAsync(id);
            if (channel == null)
                throw new ArgumentException($"Communication channel with id {id} not found.");

            channel.UpdateConfiguration(configuration);
            await _channelRepository.UpdateAsync(channel);

            return new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Type = (int)channel.Type,
                Protocol = (int)channel.Protocol,
                Role = (int)channel.Role,
                Status = (int)channel.Status,
                Enable = channel.Enable,
                CreateTime = channel.CreateTime,
                Configuration = channel.Configuration
            };
        }

        public async Task<ChannelDto> UpdateAsync(int id, UpdateChannelDto dto)
        {
            var channel = await _channelRepository.GetByIdAsync(id);
            if (channel == null)
                throw new ArgumentException($"Communication channel with id {id} not found.");

            if (dto.Configuration != null)
            {
                channel.UpdateConfiguration(dto.Configuration);
            }

            await _channelRepository.UpdateAsync(channel);

            return new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Type = (int)channel.Type,
                Protocol = (int)channel.Protocol,
                Role = (int)channel.Role,
                Status = (int)channel.Status,
                Enable = channel.Enable,
                CreateTime = channel.CreateTime,
                Configuration = channel.Configuration
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var channel = await _channelRepository.GetByIdAsync(id);
            if (channel == null)
                return false;

            if (_protocolInstances.TryGetValue(id, out var protocol))
            {
                await protocol.DisconnectAsync();
                _protocolInstances.Remove(id);
            }

            await _channelRepository.DeleteAsync(channel);
            return true;
        }

        public async Task<ChannelDto> StartAsync(int id)
        {
            var channel = await _channelRepository.GetByIdAsync(id);
            if (channel == null)
                throw new ArgumentException($"Communication channel with id {id} not found.");

            if (!_protocolInstances.TryGetValue(id, out var protocol))
            {
                protocol = _protocolFactory.CreateProtocol(channel.Type.ToString(), channel.Configuration);
                await protocol.InitializeAsync(channel.Configuration);
                _protocolInstances[id] = protocol;
            }

            await protocol.ConnectAsync();
            channel.UpdateStatus(ChannelStatus.Running);
            await _channelRepository.UpdateAsync(channel);

            return new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Type = (int)channel.Type,
                Protocol = (int)channel.Protocol,
                Role = (int)channel.Role,
                Status = (int)channel.Status,
                Enable = channel.Enable,
                CreateTime = channel.CreateTime,
                Configuration = channel.Configuration
            };
        }

        public async Task<ChannelDto> StopAsync(int id)
        {
            var channel = await _channelRepository.GetByIdAsync(id);
            if (channel == null)
                throw new ArgumentException($"Communication channel with id {id} not found.");

            if (_protocolInstances.TryGetValue(id, out var protocol))
            {
                await protocol.DisconnectAsync();
            }

            channel.UpdateStatus(ChannelStatus.Stopped);
            await _channelRepository.UpdateAsync(channel);

            return new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Type = (int)channel.Type,
                Protocol = (int)channel.Protocol,
                Role = (int)channel.Role,
                Status = (int)channel.Status,
                Enable = channel.Enable,
                CreateTime = channel.CreateTime,
                Configuration = channel.Configuration
            };
        }

        public async Task<ChannelDto> UpdateStatusAsync(int id, int status)
        {
            var channel = await _channelRepository.GetByIdAsync(id);
            if (channel == null)
                throw new ArgumentException($"Communication channel with id {id} not found.");

            channel.UpdateStatus((ChannelStatus)status);
            await _channelRepository.UpdateAsync(channel);

            return new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Type = (int)channel.Type,
                Protocol = (int)channel.Protocol,
                Role = (int)channel.Role,
                Status = (int)channel.Status,
                Enable = channel.Enable,
                CreateTime = channel.CreateTime,
                Configuration = channel.Configuration
            };
        }

        public async Task<Dictionary<int, object>> GetRealtimeDataAsync(int deviceId)
        {
            var channel = await _channelRepository.GetByIdAsync(deviceId);
            if (channel == null)
                throw new ArgumentException($"Communication channel with id {deviceId} not found.");

            if (!_protocolInstances.TryGetValue(deviceId, out var protocol))
            {
                throw new InvalidOperationException($"Channel {deviceId} is not running.");
            }

            var result = new Dictionary<int, object>();
            // TODO: 实现实时数据读取逻辑
            return result;
        }

        public async Task WriteDataAsync(int pointId, object value)
        {
            // TODO: 实现数据写入逻辑
            await Task.CompletedTask;
        }

        public async Task BatchWriteDataAsync(Dictionary<int, object> pointValues)
        {
            // TODO: 实现批量数据写入逻辑
            await Task.CompletedTask;
        }

        /// <summary>
        /// 更新通道启用状态
        /// </summary>
        public async Task<ChannelDto> UpdateEnableAsync(int id, bool enable)
        {
            var channel = await _channelRepository.GetByIdAsync(id);
            if (channel == null)
                throw new ArgumentException($"Communication channel with id {id} not found.");

            channel.Enable = enable;
            await _channelRepository.UpdateAsync(channel);

            return new ChannelDto
            {
                Id = channel.Id,
                Name = channel.Name,
                Type = (int)channel.Type,
                Protocol = (int)channel.Protocol,
                Role = (int)channel.Role,
                Status = (int)channel.Status,
                Enable = channel.Enable,
                CreateTime = channel.CreateTime,
                Configuration = channel.Configuration
            };
        }
    }
} 