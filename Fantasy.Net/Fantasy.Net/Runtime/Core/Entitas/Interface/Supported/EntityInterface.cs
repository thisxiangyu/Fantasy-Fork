using System;

namespace Fantasy.Entitas.Interface
{
    /// <summary>
    /// 支持实体"多附加"，即允许当前的父级里添加多个同类型的这类子实体。
    /// 如果没有打上这个接口，意味着实体仅支持单类型附加。
    /// </summary>
    public interface IMultiAppended : IDisposable
    {
    }

    /// <summary>
    /// 支持跟随父实体一起序列化-反序列化
    /// </summary>
    public interface IFollowCRUD
    {

    }

#if FANTASY_NET

    /// <summary>
    /// 实体支持传送
    /// </summary>
    public interface ISupportedTransfer
    {
    }

#endif


    #region DbSet 服务器客户端均可用(因为可能有客户端操作本地数据库的情况)

    // 实体DbSet默认是嵌入Parent的（Mongo里面叫BSON嵌入，SQL数据库里面应该叫Owned关系），
    // 如果要独立创建DbSet，支持子集和父集之间在数据库当中以3种关系建模:
    // 1:n 关联, 单个Parent对n个子实体，外键在子实体上 ;
    // m:n 关联，m个Parent对n个子实体 (给定父级多对多)，多个外键在子实体上;
    // 引用关联，未知个Parent对n个子实体 (不定父级多对多) ，没有外键， 仅通过引用Id索引。


    /// <summary>
    /// 在数据库中将该实体存为独立的存储集, 建立一个对父级DbSet的纯引用, 但不建立关系型引用。
    /// 注: "纯引用"未建立DbSet的关系型引用,这 意味着在数据库操作中不能发挥关系型查询, 需要手动用代码维护实体关系, 且会影响跨表/ 跨集合进行聚合时的性能。
    /// 该接口类型是 IDbSetRef 系列泛型的基类型。
    /// 如果希望父子实体之间的 DbSet引用是关系型引用, 请使用 泛型 IDbSetRef 。
    /// </summary>
    public interface IDbSetRef
    {

    }

    /// <summary>
    /// 在数据库中将该实体存为独立的存储集,并建立关系型引用: 显式指定一个带关系的父级实体类型作为DbSet引用。
    /// </summary>
    public interface IDbSetRef<T>: IDbSetRef where T : Entity
    {

    }

    /// <summary>
    /// 在数据库中将该实体存为独立的存储集,并显式指定2个带关系的父级实体类型作为DbSet引用。
    /// </summary>
    public interface IDbSetRef<T1,T2>: IDbSetRef where T1 : Entity where T2 : Entity
    {

    }

    /// <summary>
    /// 在数据库中将该实体存为独立的存储集,并显式指定3个带关系的父级实体类型作为DbSet引用。
    /// </summary>
    public interface IDbSetRef<T1, T2, T3>: IDbSetRef where T1 : Entity where T2 : Entity where T3 : Entity
    {

    }

    /// <summary>
    /// 实体在数据库中存为相对于父级实体的外部存储集, 并且不显式地指定两个父级实体类型。
    /// 多对多
    /// 如果仅为1：N的关系，请为子实体类实现 IDbSetRef 的泛型接口，显式地指定父级类型，并显式声明 ForeignKey1 属性。
    /// </summary>
    public interface IForeignDbSetTo
    {

    }
    //public class Entity: Entity
    //{
    //    long id;
    //    Entity parent;
    //    long parentId; //作为外键
    //    Dictionary<long, Entity> children;
    //}
    //public class Parent:Entity
    //{

    //}
    //public class Child : Entity
    //{

    //}

    /// <summary>
    /// 实体在数据库中存为相对于父级实体的外部存储集, 并且不显式地指定任何父级实体类型。
    /// 通过DbSet引用的方式与父级实体关联起来。
    /// 注意 : 如果直接实现这一接口, 意味着任何实体类型都有可能作为父级，这是一种不确定父级实体类型的"多对多"关系。
    /// 在关系型数据库中，它通常被认为是一种反模式（anti-pattern），因为它破坏了关系型数据库的核心优势。
    /// 除非不得不这么做，一般情况下，这种不定父级的"多对多"是不被鼓励的。
    /// 它造成的结果是需要在代码中手动维护存储集之间的关系，以及关系型查询的性能下降。
    /// （仅适用于根本无法在设计阶段确定父实体类型、需要动态关联多个上级集合的场景。）
    /// </summary>
    public interface IDbSetReference
    {
        /// <summary>
        /// 框架会自动用Parent的Id作为存储集引用，只需实现即可,无需手动赋值
        /// </summary>
        public long ReferenceDbSet { get; set; }
    }

    #endregion

}