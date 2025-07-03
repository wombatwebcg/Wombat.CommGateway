using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Wombat.CommGateway.Domain.Common
{
    /// <summary>
    /// 实体基类
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        /// 
		[Key, Column(Order = 1)]
        public int Id { get; set; }
    }
} 