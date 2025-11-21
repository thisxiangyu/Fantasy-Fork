#if FANTASY_NET
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy.Database.Attributes
{
    /// <summary>
    /// 子实体在逻辑上逻辑被视作父实体的什么? 这个枚举用于增加精细控制力, 
    /// 比如 调用数据库饥渴操作的方法时能更好地控制加载或保存实体。
    /// </summary>
    [Flags]
    public enum ToParentIs : byte
    {
        /// <summary>
        /// 未指定
        /// </summary>
        UnSet = 0,
        /// <summary>
        /// 基于组件的关系, 子实体被理解为组件, 作为父级的一部分存在
        /// </summary>
        Component,
        /// <summary>
        /// 子实体只是和父级连接, 被理解为单独的子实体, 而非父级的一个组成部分
        /// </summary>
        Child
    }

    /// <summary>
    /// [DbSet] ，标记一个实体类型允许存入数据库集合当中。设计用于加强对数据库操作的管理。
    /// 在SQL数据库中作用于Table，而在NoSQL如MongoDb中作用于Collection。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class DbSetAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DbSetAttribute() { }

        /// <summary>
        /// 存储集名字, 在Table或Collection中实体存储呈现的名字。可选设置。
        /// </summary>
        public string? Name;

        /// <summary>
        /// 存储集需按照"命名空间"划分, 避免实体同名。不同数据库的作用逻辑不同, 
        /// 比如MongoDb是给集合名添加命名空间前缀, 而PgSQL是划分到不同的Scheme当中。
        /// </summary>
        public bool WithNamespace = false;

        /// <summary>
        /// 存储集的备注
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// 数据库权限选择, 默认为任意, 即所有数据库均可以操作这个类。
        /// </summary>
        public DatabaseType DbSelection = DatabaseType.Any;

        /// <summary>
        /// 以文档式存储。即不以表格的形式存储, 而是转换为JSONB。
        /// <para>
        /// 仅针对表格式数据库, 文档式数据库天然是以文档形式存储的。
        /// </para>
        /// </summary>
        public bool IsAsDocument = false;

        /// <summary>
        /// 以嵌入式存储。即不构成独立的存储集, 而是被存为他者存储集的一部分 ( 比如在Mongo中存为BSON的部分, 在PgSQL中存为JSONB的部分 )。
        /// <para>
        /// 默认为 <see langword="false"/> , 表示独立存储。如果设置为<see langword="true"/>, 那么<see cref="IsAsDocument"/>也会被框架自动视作<see langword="true"/>, 即不会以表格形式、而是以文档形式存储了。
        /// </para>
        /// </summary>
        public bool IsEmbedded = false;

        /// <summary>
        /// 以字节流的形式存储。是以纯粹不可读的<see langword="byte"/>s的形式存入数据库中。
        /// 读存性能较普通的表格或文档形式更快, 但是牺牲了可查询性, 适合用于仅需单点查询、极致性能优先、非数据库操作、快照、压缩块等场景。
        /// </summary>
        public bool IsAsBytes = false;

        /// <summary>
        /// DbSet的实体与父实体的逻辑关系，用于过滤DbSet，从而控制从数据库中饥渴存取实体的精细度。
        /// </summary>
        public ToParentIs Relationship = ToParentIs.UnSet;

        #region TODO-LIST 这几个特性以后做
        ///// <summary>
        ///// 禁止整表更新
        ///// </summary>
        //public bool NoBulkUpdate { get; set; } = false;

        ///// <summary>
        ///// 禁止整表删除
        ///// </summary>
        //public bool NoBulkDelete { get; set; } = false;

        ///// <summary>
        ///// 是否启用分表
        ///// </summary>
        //public bool EnableSharding { get; set; } = false;
        #endregion

        /// <summary>
        /// 检查标签中的数据库选项是否包含某种数据库
        /// </summary>
        /// <returns></returns>
        public bool IfSelectionContainsDbType(DatabaseType dbType)
        {
            return (DbSelection & DatabaseType.MongoDB) == DatabaseType.MongoDB;
        }
    }
}
#endif
