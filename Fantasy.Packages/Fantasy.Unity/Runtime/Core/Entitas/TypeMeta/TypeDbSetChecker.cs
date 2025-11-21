#if FANTASY_NET
using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using Fantasy.Database;
using Fantasy.Database.Attributes;
using Fantasy.Database.Helper;

namespace Fantasy.Entitas.TypeMeta
{
    /// <summary>
    /// 类型DbSet特性静态信息缓存类
    /// </summary>
    public static class TypeDbSetInfos
    {
        /// <summary>
        /// 类型支持特性的缓存信息
        /// </summary>
        public static FrozenDictionary<long, TypeDbSetCache>? InfoByHash;

        /// <summary>
        /// 预热，执行一次，就会缓存所有检查结果。自动检查是否已预热过, 不会重复执行。
        /// </summary>
        public static void WarmUp(IEnumerable<Type> types)
        {
            if (InfoByHash != null)
                return;
            WarmUpInternal(types);
        }

        /// <summary>
        /// 重新预热
        /// </summary>
        /// <param name="types"></param>
        public static void ReWarmUp(IEnumerable<Type> types)
        {
            WarmUpInternal(types); 
        }

        private static void WarmUpInternal(IEnumerable<Type> types) {
            Dictionary<long, TypeDbSetCache> dict = new();
            foreach (var type in types)
            {
                TypeDbSetCache cache = new();

                //解析并缓存DbSet标签信息
                cache.DbSetAttri = DbSetMetadataHelper.GetDbSetAttribute(type);
                if (cache.DbSetAttri == null)
                    continue;

                //规范嵌入标记
                if (cache.DbSetAttri.IsEmbedded)
                    cache.DbSetAttri.IsAsDocument = true;

                dict.Add(TypeHashCache.GetHashCode(type), cache);
            }
            InfoByHash = dict.ToFrozenDictionary(); //转为冻结字典
        }

        /// <summary>
        /// 获取类型的支持特性信息 ( 需要先确保已经预热读取过 )
        /// </summary>
        public static TypeDbSetCache GetWarmInfo(Type type)
        {
            if (InfoByHash == null)
                // 如果还没预热，抛异常
                throw new InvalidOperationException($"TypeDbSetInfos is not warmuped.");

            if (!InfoByHash.TryGetValue(TypeHashCache.GetHashCode(type), out var info))
                throw new InvalidOperationException($" Type ({type.Name}) is not contained。");

            return info;
        }
    }

    /// <summary>
    /// 实体类型支持性的编译时检查器。
    /// </summary>
    public class TypeDbSetCache
    {
        /// <summary>
        /// 获取实体类型的标签[DbSet], 如果不存在则为null。
        /// </summary>
        public DbSetAttribute? DbSetAttri { get; internal set; }
        /// <summary>
        /// 返回是否支持[DbSet]标签。
        /// </summary>
        /// <returns></returns>
        public bool IsDbSet() { return DbSetAttri != null; }
        /// <summary>
        /// 返回是否是嵌入式存储。
        /// </summary>
        /// <returns></returns>
        public bool IsEmbedded()
        {
            if(DbSetAttri==null)
                return false;

            if (DbSetAttri.IsEmbedded)
                DbSetAttri.IsAsDocument = true;

            return DbSetAttri.IsEmbedded;
        } 
    }

    /// <summary>
    /// 类型支持特性检查器。泛型检查版本。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class TypeDbSetChecker<T> where T : Entity
    {
        /// <summary>
        /// 获取实体类型的标签[DbSet], 如果不存在则为null。
        /// </summary>
        public static DbSetAttribute? DbSetAttri => _info.DbSetAttri ?? null;
        /// <summary>
        /// 返回是否支持[DbSet]标签。
        /// </summary>
        /// <returns></returns>
        public static bool IsDbSet() { return _info.DbSetAttri != null; }
        /// <summary>
        /// 返回是否是嵌入式存储。
        /// </summary>
        /// <returns></returns>
        public static bool IsEmbedded() { return _info.IsEmbedded(); }

        private static readonly TypeDbSetCache _info;

        static TypeDbSetChecker()
        {
            _info = TypeDbSetInfos.GetWarmInfo(typeof(T));
        }
    }
}
#endif