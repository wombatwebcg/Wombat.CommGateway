using Microsoft.Extensions.DependencyInjection;



namespace Wombat.CommGateway.API
{
    [AutoInject(ServiceLifetime = ServiceLifetime.Scoped)]
    public class RequestBody
    {
        public string Body { get; set; }
    }
}
