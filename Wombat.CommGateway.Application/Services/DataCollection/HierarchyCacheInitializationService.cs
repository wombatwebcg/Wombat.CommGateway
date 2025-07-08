using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wombat.Extensions.AutoGenerator.Attributes;

namespace Wombat.CommGateway.Application.Services.DataCollection
{
    /// <summary>
    /// 层级关系缓存初始化服务
    /// 在应用程序启动时自动初始化订阅管理器的层级关系缓存
    /// </summary>
    [AutoInject(ServiceLifetime = ServiceLifetime.Singleton)]
    public class HierarchyCacheInitializationService : BackgroundService
    {
        private readonly ILogger<HierarchyCacheInitializationService> _logger;
        private readonly ISubscriptionManager _subscriptionManager;

        public HierarchyCacheInitializationService(
            ILogger<HierarchyCacheInitializationService> logger,
            ISubscriptionManager subscriptionManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 等待其他服务初始化完成
            await Task.Delay(5000, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                _logger.LogInformation("开始初始化订阅管理器层级关系缓存...");

                await _subscriptionManager.RefreshHierarchyCacheAsync();

                _logger.LogInformation("订阅管理器层级关系缓存初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化订阅管理器层级关系缓存失败");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("停止层级关系缓存初始化服务");
            await base.StopAsync(cancellationToken);
        }
    }
} 