using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wombat.CommGateway.Infrastructure.Utils
{
    public static class AsyncDisposable
    {
        public static IAsyncDisposable Create(Func<ValueTask> disposeAsync)
        {
            return new AsyncDisposableImpl(disposeAsync);
        }

        private class AsyncDisposableImpl : IAsyncDisposable
        {
            private readonly Func<ValueTask> _disposeAsync;
            public AsyncDisposableImpl(Func<ValueTask> disposeAsync)
            {
                _disposeAsync = disposeAsync ?? throw new ArgumentNullException(nameof(disposeAsync));
            }

            public ValueTask DisposeAsync()
            {
                return _disposeAsync();
            }
        }
    }
}
