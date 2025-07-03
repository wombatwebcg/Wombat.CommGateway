using Microsoft.Extensions.DependencyInjection;
using Wombat.Extensions.AutoGenerator.Attributes;



namespace Wombat.CommGateway.API
{
    [AutoInject(typeof(RequestBody), ServiceLifetime = ServiceLifetime.Scoped)]
    public class RequestBody
    {
        public string Body { get; set; }
    }
}
