using NJ = Newtonsoft.Json;
using MongoDB.Bson.Serialization.Attributes;
using MemoryPack;
using System.Runtime.Serialization;
using LightProto;
using System;

#if FANTASY_UNITY
namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    /// 这个标签主要是用来占位, 避免客户端报错
    /// </summary>
    public class NotMappedAttribute: Attribute
    {

    }
}
#endif

#if FANTASY_NET
using MJ = System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
#endif

namespace Fantasy.Database.Attributes
{
    /// <summary>
    /// DbSet接口, 标记一个实体类型允许存入数据库集合当中。设计用于加强对数据库操作的管理。
    /// </summary>
    public interface IDbSet
    {
        /// <summary>
        /// 设置数据库存储选项
        /// </summary>
        public DbSetOptions? DbSetOpts { get; }

        /// <summary>
        /// 检查标签中的数据库选项是否包含某种数据库
        /// </summary>
        /// <returns></returns>
        public bool IfSelectionContainsDbType(DatabaseType dbType)
        {
            return (DbSetOpts?.DbSelection & DatabaseType.MongoDB) == DatabaseType.MongoDB;
        }
    }
    /// <summary>
    /// 设置数据库存储选项
    /// </summary>
    public class DbSetOptions
    {
        /// <summary>
        /// 存储集名字, 在Table或Collection中实体存储呈现的名字。可选设置。
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public string? Name;

        /// <summary>
        /// 存储集需按照"命名空间"划分, 避免实体同名。不同数据库的作用逻辑不同, 
        /// 比如MongoDb是给集合名添加命名空间前缀, 而PgSQL是划分到不同的Scheme当中。
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public bool WithNamespace = false;

        /// <summary>
        /// 存储集的备注
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public string? Comment { get; set; }

        /// <summary>
        /// 数据库权限选择, 默认为任意, 即所有数据库均可以操作这个类。
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public DatabaseType DbSelection = DatabaseType.Any;

        /// <summary>
        /// 以文档式存储。即不以表格的形式存储, 而是转换为JSONB。
        /// <para>
        /// 仅针对表格式数据库, 文档式数据库天然是以文档形式存储的。
        /// </para>
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public bool IsAsDocument = false;

        /// <summary>
        /// 以嵌入式存储。即不构成独立的存储集, 而是被存为他者存储集的一部分 ( 比如在Mongo中存为BSON的部分, 在PgSQL中存为JSONB的部分 )。
        /// <para>
        /// 默认为 <see langword="false"/> , 表示独立存储。如果设置为<see langword="true"/>, 那么<see cref="IsAsDocument"/>也会被框架自动视作<see langword="true"/>, 即不会以表格形式、而是以文档形式存储了。
        /// </para>
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public bool IsEmbedded = false;

        /// <summary>
        /// 以字节流的形式存储。是以纯粹不可读的<see langword="byte"/>s的形式存入数据库中。
        /// 读存性能较普通的表格或文档形式更快, 但是牺牲了可查询性, 适合用于仅需单点查询、极致性能优先、非数据库操作、快照、压缩块等场景。
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public bool IsAsBytes = false;

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
        /// 数据库权限选择。默认为任意, 即所有数据库均可以操作这个类。
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
        /// 将存储集标记为以配置形式存储, 这意味着这份数据应单独划归读写权限, 
        /// 由策划或开发者来定义数据内容, 配置数据随运营版本发布, 玩家不会写入数据到配置中。
        /// 注意 : <see cref="IsAsConfig"/>需要配合在启服配置中
        /// 将一个数据库的名字设置为"<see cref="DatabaseSetting.ConfigDbName"/>"才能被识别,
        /// 如果没有设置任何配置表数据库, 该存储集将会被忽视。
        /// </summary>
        public bool IsAsConfig = false;

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
            if (dbType == DatabaseType.None)
                return false;

            return (DbSelection & dbType) == dbType;
        }
    }
}
