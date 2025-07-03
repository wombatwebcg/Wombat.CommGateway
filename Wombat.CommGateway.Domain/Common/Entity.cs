

using FreeSql.DataAnnotations;

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
        [Column(IsPrimary = true, IsIdentity = true)]
        public int Id { get; set; }
    }
} 