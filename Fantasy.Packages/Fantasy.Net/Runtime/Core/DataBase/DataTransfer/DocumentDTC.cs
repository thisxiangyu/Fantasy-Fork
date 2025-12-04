#if FANTASY_NET
using System.ComponentModel.DataAnnotations;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Pool;

namespace Fantasy.Database.DataTransfer
{
    /// <summary>
    /// 针对 <see cref="DbSetAttribute.IsAsDocument"/> 设置为 <see langword="true"/> 的对象需要用这个类的实例帮助存取数据。
    /// 仅表格型数据库如 <see cref="PostgreSQL"/> 数据库以文档形式存储的记录需要如此。
    /// </summary>
    public class DocumentDTC : IPool, IDisposable
    {
        /// <summary>
        /// 转送Json, 仅支持实体
        /// </summary>
        public object? Json { get; set; }
        ///// <summary>
        ///// 转送二进制字节
        /////  Note:暂不支持因为有点麻烦
        ///// </summary>
        //public byte[]? Bytes { get; set; }

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
        public virtual void Dispose()
        {
            Json = null;
        }
    }

    /// <summary>
    /// 实体文档存储转送类
    /// </summary>
    public class EntityDocumentDTC : DocumentDTC
    {
        /// <summary>
        /// 转送父级的Id
        /// </summary>
        public long ParentId { get; set; }
        /// <summary>
        /// 转送父级的类型
        /// </summary>
        public long ParentType { get; set; }
    }
}
#endif