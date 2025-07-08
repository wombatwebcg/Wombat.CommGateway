using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wombat.CommGateway.Application.Interfaces;
using Wombat.CommGateway.Infrastructure.Utils;
using Wombat.Extensions.AutoGenerator.Attributes;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    public interface IDataPushBus : IAsyncDisposable
    {
        ValueTask PublishAsync(object message);
        IAsyncDisposable RegisterAsync(Func<object, Task> handler);
    }

    [AutoInject<IDataPushBus>(ServiceLifetime = ServiceLifetime.Singleton)]
    public class DataPushBus : IDataPushBus
    {
        private readonly ConcurrentDictionary<Guid, Func<object, Task>> _handlers = new();

        public ValueTask PublishAsync(object msg)
        {
            var tasks = _handlers.Values.Select(h => h(msg));
            return new ValueTask(Task.WhenAll(tasks));
        }

        public IAsyncDisposable RegisterAsync(Func<object, Task> handler)
        {
            var id = Guid.NewGuid();
            _handlers[id] = handler;
            return AsyncDisposable.Create(() => { 
                _handlers.TryRemove(id, out _);
                return ValueTask.CompletedTask;
            });
        }

        public ValueTask DisposeAsync() => default;
    }

}
