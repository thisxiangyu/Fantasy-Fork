#if FANTASY_NET
using System.Collections.Generic;
using Fantasy.Entitas.TypeMeta;
using Fantasy.Helper;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Assembly
{
    /// <summary>
    /// DbSet模型构建注册器接口。
    /// 由 Source Generator (DbSetRegistrationGenerator) 自动生成实现类，
    /// 用于在编译时将程序集中所有 [DbSet] 类型注册到 EFCore 的 ModelBuilder 中，
    /// 同时提供编译时生成的 TypeDbSetCache 字典。
    /// </summary>
    public interface IDbSetModelBuilderRegistrar
    {
        /// <summary>
        /// 将本程序集中所有 [DbSet] 类型注册到 EFCore ModelBuilder 中。
        /// 此方法由源生成器在编译时生成，包含所有 modelBuilder.Entity() 等调用。
        /// </summary>
        void RegisterToModelBuilder(ModelBuilder modelBuilder, bool isSessionForConfig, JsonSettings jsonSettings);

        /// <summary>
        /// 获取本程序集中所有 [DbSet] 类型的编译时缓存字典。
        /// </summary>
        Dictionary<long, TypeDbSetCache> GetDbSetTypeCache();
    }
}
#endif