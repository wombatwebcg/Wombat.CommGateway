using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    public interface ISubscriptionManager
    {
        void Add(string connectionKey, int itemId);     
        void Remove(string connectionKey, int itemId);
        IReadOnlyList<int> Get(string connectionKey);
        IReadOnlyList<string> GetConnectionsByItem(int itemId);
        void RemoveConnection(string connectionKey);

        IReadOnlyList<string> GetAllConnections();
    }

    [AutoInject<ISubscriptionManager>(ServiceLifetime = ServiceLifetime.Singleton)]

    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly ConcurrentDictionary<string, HashSet<int>> _map = new();

        public void Add(string key, int id)
            => _map.AddOrUpdate(key, _ => new(id) { id }, (_, set) => { set.Add(id); return set; });

        public void Remove(string key, int id)
        {
            if (_map.TryGetValue(key, out var set)) set.Remove(id);
        }

        public IReadOnlyList<int> Get(string key) => _map.TryGetValue(key, out var set)
            ? set.ToList() : Array.Empty<int>();

        public IReadOnlyList<string> GetConnectionsByItem(int id)
            => _map.Where(p => p.Value.Contains(id)).Select(p => p.Key).ToList();

        public void RemoveConnection(string key) => _map.TryRemove(key, out _);

        public IReadOnlyList<string> GetAllConnections()=> _map.Select(p => p.Key).ToList();
    }
}
