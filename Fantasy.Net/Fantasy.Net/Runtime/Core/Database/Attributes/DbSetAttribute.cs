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
        JustLinking
    }

    /// <summary>
    /// [DbSet] ，标记一个实体类型允许存入数据库集合当中。设计用于加强对数据库操作的管理。
    /// 在SQL数据库中作用于Table，而在NoSQL如MongoDb中作用于Collection。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class DbSetAttribute :Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DbSetAttribute(){}

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
