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

#if FANTASY_NET

    /// <summary>
    /// 实体支持传送
    /// </summary>
    public interface ISupportedTransfer
    {
    }

#endif

}