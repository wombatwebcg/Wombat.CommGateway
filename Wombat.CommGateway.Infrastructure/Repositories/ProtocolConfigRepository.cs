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
    /// 协议配置仓储实现
    /// </summary>
    [AutoInject(typeof(IProtocolConfigRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class ProtocolConfigRepository : BaseRepository<ProtocolConfig, GatawayDB>, IProtocolConfigRepository
    {
        private readonly IServiceProvider _service;

        public ProtocolConfigRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<List<ProtocolConfig>> GetAllAsync()
        {
            return await Select.ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<ProtocolConfig> GetByIdAsync(int id)
        {
            return await Select.Where(p => p.Id == id).FirstAsync();
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

        /// <inheritdoc/>
        public async Task<ProtocolConfig> GetByNameAsync(string name)
        {
            return await Select.Where(p => p.Name == name).FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ProtocolConfig>> GetEnabledConfigsAsync()
        {
            return await Select.Where(p => p.Enable).ToListAsync();
        }
    }
} 