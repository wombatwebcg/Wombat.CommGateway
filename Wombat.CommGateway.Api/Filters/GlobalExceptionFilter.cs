using Wombat.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Wombat.CommGateway.API;


namespace Wombat.CommGateway.API.Filters
{

    public class GlobalExceptionFilter : BaseActionFilterAsync
    {
        readonly ILogger _logger;
        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            Exception ex = context.Exception;

            if (ex is BusException busEx)
            {
                _logger.LogInformation(busEx.Message);
                context.Result = Error(busEx.Message, busEx.ErrorCode);
            }
            else
            {
                _logger.LogError(ex, "");
                context.Result = Error("系统繁忙");
            }

            await Task.CompletedTask;
        }
    }
}
