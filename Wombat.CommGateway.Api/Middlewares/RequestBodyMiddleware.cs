using Wombat;
using Wombat.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore;

namespace Wombat.CommGateway.API
{
    public class RequestBodyMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestBodyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if ((context.Request.ContentType ?? string.Empty).Contains("application/json"))
            {
                context.Request.EnableBuffering();

                using (var memoryStream = new MemoryStream())
                {
                    // 保存原始流的位置
                    var originalPosition = context.Request.Body.Position;

                    await context.Request.Body.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    string body = Encoding.UTF8.GetString(memoryStream.ToArray());
                    context.RequestServices.GetRequiredService<RequestBody>().Body = body;

                    // 双重保障：恢复原始流的位置
                    context.Request.Body.Position = originalPosition;
                }
            }

            await _next(context);
        }
    }
}
