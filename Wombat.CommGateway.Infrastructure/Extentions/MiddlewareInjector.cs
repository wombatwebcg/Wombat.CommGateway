using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace   Wombat.CommGateway.Infrastructure.Extentions
{
    public static partial class Extention
    {
        public static IApplicationBuilder UseMiddlewareInjector(this IApplicationBuilder builder, MiddlewareInjectorOptions options)
        {
            return builder.UseMiddleware<MiddlewareInjector>(builder.New(), options);
        }
    }

    public class MiddlewareInjector
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _builder;
        private readonly MiddlewareInjectorOptions _options;
        private RequestDelegate _subPipeline;

        public MiddlewareInjector(RequestDelegate next, IApplicationBuilder builder, MiddlewareInjectorOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Invoke(HttpContext httpContext)
        {
            var injector = _options.GetInjector();
            if (injector != null)
            {
                var builder = _builder.New();
                injector(builder);
                builder.Run(_next);
                _subPipeline = builder.Build();
            }

            if (_subPipeline != null)
            {
                return _subPipeline(httpContext);
            }

            return _next(httpContext);
        }
    }

    public class MiddlewareInjectorOptions
    {
        private Action<IApplicationBuilder> _injector;

        public void InjectMiddleware(Action<IApplicationBuilder> builder)
        {
            Interlocked.Exchange(ref _injector, builder);
        }

        internal Action<IApplicationBuilder> GetInjector()
        {
            return Interlocked.Exchange(ref _injector, null);
        }
    }
}
