using Wombat.CommGateway.API.Modules;

namespace Wombat.CommGateway.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 使用API模块，一键启用所有API功能
            builder.AddApiModule(options =>
            {
                // 可以在这里自定义配置选项
                // 例如：options.Middleware.EnableSwagger = false;
            });

            var app = builder.Build();

            // 使用API模块中间件管道
            app.UseApiModule(builder.Configuration);

            app.Run();
        }
    }
}
