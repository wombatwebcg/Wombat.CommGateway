using System.Threading.Tasks;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Interfaces
{
    /// <summary>
    /// 点位变更通知服务接口
    /// 用于在点位配置发生变更时通知数据采集服务
    /// </summary>
    public interface IPointChangeNotificationService
    {
        /// <summary>
        /// 通知点位已创建
        /// </summary>
        /// <param name="point">新创建的点位</param>
        /// <returns>通知处理结果</returns>
        Task OnPointCreatedAsync(DevicePoint point);

        /// <summary>
        /// 通知点位已更新
        /// </summary>
        /// <param name="point">更新后的点位</param>
        /// <param name="oldScanRate">更新前的扫描周期</param>
        /// <param name="oldEnable">更新前的启用状态</param>
        /// <returns>通知处理结果</returns>
        Task OnPointUpdatedAsync(DevicePoint point, int? oldScanRate = null, bool? oldEnable = null);

        /// <summary>
        /// 通知点位已删除
        /// </summary>
        /// <param name="pointId">被删除的点位ID</param>
        /// <returns>通知处理结果</returns>
        Task OnPointDeletedAsync(int pointId);

        /// <summary>
        /// 通知点位启用状态变更
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <param name="enabled">新的启用状态</param>
        /// <returns>通知处理结果</returns>
        Task OnPointEnabledChangedAsync(int pointId, bool enabled);

        /// <summary>
        /// 批量通知点位导入
        /// </summary>
        /// <param name="points">导入的点位列表</param>
        /// <returns>通知处理结果</returns>
        Task OnPointsBatchImportedAsync(DevicePoint[] points);
    }
} 