using System;
using System.Collections.Generic;
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
    /// 协议配置服务实现
    /// </summary>
    /// 
    [AutoInject(typeof(IProtocolConfigService))]
    public class ProtocolConfigService : IProtocolConfigService
    {
        private readonly IProtocolConfigRepository _protocolConfigRepository;

        public ProtocolConfigService(IProtocolConfigRepository protocolConfigRepository)
        {
            _protocolConfigRepository = protocolConfigRepository;
        }

        public async Task<List<ProtocolConfigDto>> GetListAsync()
        {
            var configs = await _protocolConfigRepository.GetAllAsync();
            return configs.ConvertAll(config => new ProtocolConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                Type = config.Type,
                Version = config.Version,
                Parameters = config.Parameters,
                IsEnabled = config.IsEnabled,
                CreateTime = config.CreateTime,
                UpdateTime = config.UpdateTime
            });
        }

        public async Task<ProtocolConfigDto> GetByIdAsync(int id)
        {
            var config = await _protocolConfigRepository.GetByIdAsync(id);
            if (config == null)
                return null;

            return new ProtocolConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                Type = config.Type,
                Version = config.Version,
                Parameters = config.Parameters,
                IsEnabled = config.IsEnabled,
                CreateTime = config.CreateTime,
                UpdateTime = config.UpdateTime
            };
        }

        public async Task<ProtocolConfigDto> CreateAsync(CreateProtocolConfigRequest request)
        {
            var config = new ProtocolConfig(request.Name, request.Type, request.Version);
            if (request.Parameters != null)
            {
                config.UpdateParameters(request.Parameters);
            }

            await _protocolConfigRepository.InsertAsync(config);

            return new ProtocolConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                Type = config.Type,
                Version = config.Version,
                Parameters = config.Parameters,
                IsEnabled = config.IsEnabled,
                CreateTime = config.CreateTime,
                UpdateTime = config.UpdateTime
            };
        }

        public async Task<ProtocolConfigDto> UpdateAsync(int id, UpdateProtocolConfigRequest request)
        {
            var config = await _protocolConfigRepository.GetByIdAsync(id);
            if (config == null)
                throw new ArgumentException($"Protocol config with id {id} not found.");

            if (request.Parameters != null)
            {
                config.UpdateParameters(request.Parameters);
            }

            config.UpdateStatus(request.IsEnabled);

            await _protocolConfigRepository.UpdateAsync(config);

            return new ProtocolConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                Type = config.Type,
                Version = config.Version,
                Parameters = config.Parameters,
                IsEnabled = config.IsEnabled,
                CreateTime = config.CreateTime,
                UpdateTime = config.UpdateTime
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var config = await _protocolConfigRepository.GetByIdAsync(id);
            if (config == null)
                return false;

            await _protocolConfigRepository.DeleteAsync(config);
            return true;
        }

        public async Task<ProtocolConfigDto> UpdateStatusAsync(int id, bool isEnabled)
        {
            var config = await _protocolConfigRepository.GetByIdAsync(id);
            if (config == null)
                throw new ArgumentException($"Protocol config with id {id} not found.");

            config.UpdateStatus(isEnabled);
            await _protocolConfigRepository.UpdateAsync(config);

            return new ProtocolConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                Type = config.Type,
                Version = config.Version,
                Parameters = config.Parameters,
                IsEnabled = config.IsEnabled,
                CreateTime = config.CreateTime,
                UpdateTime = config.UpdateTime
            };
        }
    }
} 