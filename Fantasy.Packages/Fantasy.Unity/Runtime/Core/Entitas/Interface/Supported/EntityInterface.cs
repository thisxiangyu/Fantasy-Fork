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
    /// 实体支持序列化。当实体标记为<see cref="Database.Attributes.DbSetAttribute"/>时, 必然为支持序列化, 
    /// 但是当未标记为<see cref="Database.Attributes.DbSetAttribute"/>时, 说明可以序列化(用于传输或自行持久化)
    /// 但是不一定存入数据库。
    /// </summary>
    public interface ISupportedSerialize
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

}