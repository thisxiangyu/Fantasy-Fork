#if FANTASY_NET
using System.ComponentModel.DataAnnotations;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Pool;

namespace Fantasy.Database.DTO
{
    /// <summary>
    /// <see cref="EntityDocumentDTO"/>的对象池
    /// </summary>
    public class PoolStackOfEntityDocumentDTO : PoolStack<EntityDocumentDTO>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
       public PoolStackOfEntityDocumentDTO() : base(4096) { }

        /// <summary>
        /// 返还
        /// </summary>
        /// <param name="item"></param>
        public override void Return(EntityDocumentDTO item)
        {
            item.Dispose();
            base.Return(item);
        }
    }

    /// <summary>
    /// 针对 <see cref="DbSetAttribute.IsAsDocument"/> 设置为 <see langword="true"/> 的实体需要用这个类的实例帮助Dapper读取EFCore存储的数据。
    /// 仅表格型数据库如 <see cref="PostgreSQL"/> 数据库以文档形式存储的记录且以Dapper模式读取时需要如此操办。
    /// </summary>
    public class EntityDocumentDTO : IPool, IDisposable
    {
        /// <summary>
        /// 转送<see cref="Entity.Id"/>
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 转送父级的Id
        /// </summary>
        public long ParentId { get; set; }
        /// <summary>
        /// 转送父级的类型
        /// </summary>
        public long ParentType { get; set; }
        /// <summary>
        /// 转送实体Json
        /// </summary>
        public string? Json { get; set; }
        /// <summary>
        /// 转送二进制字节
        /// </summary>
        public byte[]? Bytes { get; set; }

        private bool _isPool;

        /// <summary>
        /// 获取一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <returns></returns>
        public bool IsPool()
        {
            return _isPool;
        }

        /// <summary>
        /// 设置一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <param name="isPool"></param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
            Id = 0; 
            ParentId=0; 
            ParentType= 0;
        }
    }
}
#endif