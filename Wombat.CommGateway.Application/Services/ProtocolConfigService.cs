using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Domain.Repositories;
using AutoMapper;

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
        private readonly IMapper _mapper;
        public ProtocolConfigService(IProtocolConfigRepository protocolConfigRepository,IMapper mapper)
        {
            _protocolConfigRepository = protocolConfigRepository ?? throw new ArgumentNullException(nameof(protocolConfigRepository));
            _mapper = mapper ?? throw new ArgumentNullException();
        }

        public async Task<List<ProtocolConfigDto>> GetListAsync()
        {
            var configs = await _protocolConfigRepository.GetAllAsync();
            return configs.Select(MapToDto).ToList();
        }

        public async Task<ProtocolConfigDto> GetByIdAsync(int id)
        {
            var config = await _protocolConfigRepository.GetByIdAsync(id);
            return config == null ? null : MapToDto(config);
        }

        public async Task<ProtocolConfigDto> CreateAsync(CreateProtocolConfigDto dto)
        {
            var config = new ProtocolConfig(dto.Name, ParseProtocolType(dto.Type), dto.Version);
            config.Parameters = dto.Parameters ?? new Dictionary<string, string>();
            await _protocolConfigRepository.InsertAsync(config);
            return MapToDto(config);
        }

        public async Task<ProtocolConfigDto> UpdateAsync(int id, UpdateProtocolConfigDto dto)
        {
            var config = await _protocolConfigRepository.GetByIdAsync(id);
            if (config == null) throw new ArgumentException($"Protocol config with id {id} not found.");
            
            config.Name = dto.Name;
            config.Type = ParseProtocolType(dto.Type);
            config.Version = dto.Version;
            config.Parameters = dto.Parameters ?? new Dictionary<string, string>();
            config.Enable = dto.Enable;
            config.UpdateTime = DateTime.Now;
            
            await _protocolConfigRepository.UpdateAsync(config);
            return MapToDto(config);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var config = await _protocolConfigRepository.GetByIdAsync(id);
            if (config == null) return false;
            
            await _protocolConfigRepository.DeleteAsync(config);
            return true;
        }

        public async Task<ProtocolConfigDto> UpdateStatusAsync(int id, bool enabled)
        {
            var config = await _protocolConfigRepository.GetByIdAsync(id);
            if (config == null) throw new ArgumentException($"Protocol config with id {id} not found.");
            
            config.UpdateStatus(enabled);
            await _protocolConfigRepository.UpdateAsync(config);
            return MapToDto(config);
        }

        private ProtocolConfigDto MapToDto(ProtocolConfig config)
        {
            return new ProtocolConfigDto
            {
                Id = config.Id,
                Name = config.Name,
                Type = config.Type.ToString(),
                Version = config.Version,
                Parameters = config.Parameters,
                Enable = config.Enable,
                CreateTime = config.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdateTime = config.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        private ProtocolType ParseProtocolType(string type)
        {
            return type?.ToLower() switch
            {
                "modbustcp" => ProtocolType.ModbusTCP,
                "modbusrtu" => ProtocolType.ModbusRTU,
                "siemenss7" => ProtocolType.SiemensS7,
                "mitsubishimc" => ProtocolType.MitsubishiMC,
                "omronfins" => ProtocolType.OmronFINS,
                _ => ProtocolType.ModbusTCP
            };
        }
    }
} 