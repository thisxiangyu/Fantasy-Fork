using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Frozen;
using Fantasy.Database;
using Fantasy.Database.Attributes;
using Fantasy.Entitas.Interface;

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

                //解析DbSet标签
                cache.DbSetAttri = DbSetMetadataHelper.GetDbSetAttribute(type);
                if (cache.DbSetAttri == null)
                    continue;

                //解析引用
                cache.HasDbSetRef = typeof(IDbSetRef).IsAssignableFrom(type);

                //解析Parent(s)的Type
                var Parents = DbSetMetadataHelper.GetForeignKeyParentTypes(type);
                cache.DbSetParents = Parents.ToFrozenSet();
                Dictionary<long, string> fk = new();

                foreach (var parent in Parents) {
                    long ParentId = TypeHashCache.GetHashCode(parent);
                    var parentAttr = DbSetMetadataHelper.GetDbSetAttribute(parent);
                    if (parentAttr == null)
                        throw new Exception($" Unexpected: Though \"{parent}\" has no DbSetAttribute but is still trying to be set as a Parent-DbSet of \"{type}\".");

                    //解析FK
                    string foreignKeyName = $"{parent.Name}";
                    if (parentAttr != null && parentAttr.WithNamespace)
                        foreignKeyName = $"{parent.Namespace}_{parent.Name}";

                    fk.Add(ParentId, foreignKeyName);

                    //转为冻结字典
                    cache.ForeignKeyByParentHash = fk.ToFrozenDictionary();
                }
                dict.Add(TypeHashCache.GetHashCode(type), cache);
            }
            InfoByHash = dict.ToFrozenDictionary(); //转为冻结字典
        }

        /// <summary>
        /// 获取类型的支持特性信息
        /// </summary>
        public static TypeDbSetCache GetInfo(Type type)
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
        /// 是否存在DbSet引用。
        /// </summary>
        public bool HasDbSetRef { get; internal set; }
        /// <summary>
        /// 父级DbSet实体类型。
        /// </summary>
        public FrozenSet<Type>? DbSetParents { get; internal set; }
        /// <summary>
        /// 根据父级类型获取实体DbSet中的ForeignKey。
        /// </summary>
        public FrozenDictionary<long, string>? ForeignKeyByParentHash { get; internal set; }
        /// <summary>
        /// 返回是否支持[DbSet]标签。
        /// </summary>
        /// <returns></returns>
        public bool IsDbSetDbSet() { return DbSetAttri != null; }
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
        public static DbSetAttribute? DbSetAttri => _info?.DbSetAttri ?? null;
        /// <summary>
        /// 根据类型获取实体DbSet中的ForeignKey。
        /// </summary>
        public static FrozenDictionary<long, string>? ForeignKeyByTypeHash => _info?.ForeignKeyByParentHash ?? null;
        /// <summary>
        /// 返回是否支持[DbSet]标签。
        /// </summary>
        /// <returns></returns>
        public static bool IsDbSetDbSet() { return DbSetAttri != null; }

        private static readonly TypeDbSetCache _info;

        static TypeDbSetChecker()
        {
            _info = TypeDbSetInfos.GetInfo(typeof(T));
        }
    }
}
