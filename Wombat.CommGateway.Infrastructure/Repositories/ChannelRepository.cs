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
    /// 通信通道仓储实现
    /// </summary>
    [AutoInject(typeof(IChannelRepository), ServiceLifetime = ServiceLifetime.Scoped)]
    public class ChannelRepository : BaseRepository<Channel, GatawayDB>, IChannelRepository
    {
        private readonly IServiceProvider _service;

        public ChannelRepository(IServiceProvider service) : base(service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public async Task<List<Channel>> GetAllAsync()
        {
            return await Select
                .Include(c => c.ProtocolConfig)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Channel> GetByIdAsync(int id)
        {
            return await Select
                .Include(c => c.ProtocolConfig)
                .Where(c => c.Id == id)
                .FirstAsync();
        }




        /// <inheritdoc/>
        public async Task<Channel> GetByNameAsync(string name)
        {
            return await Select
                .Include(c => c.ProtocolConfig)
                .Where(c => c.Name == name)
                .FirstAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Channel>> GetByProtocolConfigIdAsync(int protocolConfigId)
        {
            if (Enum.IsDefined(typeof(ProtocolType), protocolConfigId))
            {
               var protocol = (ProtocolType)protocolConfigId;
                return await Select
                .Include(c => c.ProtocolConfig)
                .Where(c => c.Protocol == protocol)
                .ToListAsync();
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<List<Channel>> GetRunningChannelsAsync()
        {
            return await Select
                .Include(c => c.ProtocolConfig)
                .Where(c => c.Status == ChannelStatus.Running)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var device = await GetByIdAsync(id);
            if (device != null)
            {
               var count =  await base.DeleteAsync(device);
               if(count>0) return true;
            }
            return false;
        }


    }
} 