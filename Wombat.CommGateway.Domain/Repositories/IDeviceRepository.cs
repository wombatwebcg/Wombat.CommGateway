using System.Collections.Generic;
using System.Threading.Tasks;
using Wombat.Extensions.FreeSql;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Domain.Repositories
{
    /// <summary>
    /// 网关设备仓储接口
    /// </summary>
    public interface IDeviceRepository: IRepositoryKey<Device>
    {
        /// <summary>
        /// 获取所有设备
        /// </summary>
        Task<List<Device>> GetAllAsync();

        /// <summary>
        /// 根据ID获取设备
        /// </summary>
        Task<Device> GetByIdAsync(int id);



        /// <summary>
        /// 删除设备
        /// </summary>
         Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 根据名称获取设备
        /// </summary>
        Task<Device> GetByNameAsync(string name);
    }
} 