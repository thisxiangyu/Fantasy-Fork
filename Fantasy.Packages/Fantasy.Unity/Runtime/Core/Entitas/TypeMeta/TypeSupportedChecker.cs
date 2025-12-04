using System;
using System.Collections.Generic;
using Fantasy.Entitas.Interface;
using Fantasy.DataStructure.Dictionary;

namespace Fantasy.Entitas.TypeMeta
{
    /// <summary>
    /// 类型支持特性信息静态信息缓存类
    /// </summary>
    public static class TypeSupportedChecker
    {
        /// <summary>
        /// 类型支持特性的缓存信息
        /// </summary>
        public static Int64FrozenDictionary<TypeSupportedCache>? InfoByHash;

        /// <summary>
        /// 预热，执行一次，就会缓存所有检查结果。
        /// </summary>
        public static void WarmUp(IEnumerable<Type> types)
        {
            Dictionary<long, TypeSupportedCache> dict = new();
            foreach(var type in types)
            {
                dict.Add(TypeHashCache.GetHashCode(type), WarmUpOne(type));
            }
            InfoByHash = new(dict); //转为冻结字典
        }

        /// <summary>
        /// 预热单个
        /// </summary>
        internal static TypeSupportedCache WarmUpOne(Type type) {
            TypeSupportedCache cache = new();
            cache.IsMulti = typeof(IMultiAppended).IsAssignableFrom(type);
#if FANTASY_NET
            cache.IsTransfer = typeof(ISupportedTransfer).IsAssignableFrom(type);
#endif
            return cache;
        }

        /// <summary>
        /// 获取类型的支持特性信息
        /// </summary>
        public static TypeSupportedCache GetInfo(Type type) {
            if(InfoByHash == null)
                // 如果还没预热，抛异常
                throw new InvalidOperationException($"TypeSupportedInfos is not warmuped.");

            if (!InfoByHash.TryGetValue(TypeHashCache.GetHashCode(type), out var info))
                throw new InvalidOperationException($" Type ({type.Name}) is not contained。");

            return info;
        }

        internal static TypeSupportedCache? TryGetInfo(Type type)
        {
            if (InfoByHash == null)
                // 如果还没预热，抛异常
                return null;

            if (!InfoByHash.TryGetValue(TypeHashCache.GetHashCode(type), out var info))
                return null;

            return info;
        }
    }

    /// <summary>
    /// 实体类型支持性的信息缓存。
    /// </summary>
    public class TypeSupportedCache
    {
        /// <summary>
        /// 获取实体类型是否实现了 <see cref="IMultiAppended"/> 接口。
        /// 实现该接口的实体支持在父实体中添加多个同类型的组件实例。
        /// </summary>
        /// <value>
        /// 如果实体类型实现了 <see cref="IMultiAppended"/> 接口，则为 <c>true</c>；否则为 <c>false</c>。
        /// </value>
        public bool IsMulti { get; internal set; }
#if FANTASY_NET
        /// <summary>
        /// 获取实体类型是否实现了 <see cref="ISupportedTransfer"/> 接口。
        /// 实现该接口的实体支持跨进程传输（如服务器间传送）。
        /// </summary>
        /// <value>
        /// 如果实体类型实现了 <see cref="ISupportedTransfer"/> 接口，则为 <c>true</c>；否则为 <c>false</c>。
        /// </value>
        public bool IsTransfer { get; internal set; }
#endif
    }

    /// <summary>
    /// 类型支持特性检查器。泛型检查版本。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class TypeSupportedChecker<T> where T : Entity
    {
        /// <summary>
        /// 获取实体类型是否实现了 <see cref="IMultiAppended"/> 接口。
        /// 实现该接口的实体支持在父实体中添加多个同类型的组件实例。
        /// </summary>
        /// <value>
        /// 如果实体类型实现了 <see cref="IMultiAppended"/> 接口，则为 <c>true</c>；否则为 <c>false</c>。
        /// </value>
        public static bool IsMulti => _info?.IsMulti ?? false;
#if FANTASY_NET
        /// <summary>
        /// 获取实体类型是否实现了 <see cref="ISupportedTransfer"/> 接口。
        /// 实现该接口的实体支持跨进程传输（如服务器间传送）。
        /// </summary>
        /// <value>
        /// 如果实体类型实现了 <see cref="ISupportedTransfer"/> 接口，则为 <c>true</c>；否则为 <c>false</c>。
        /// </value>
        public static bool IsTransfer => _info?.IsTransfer ?? false;
#endif

        private static readonly TypeSupportedCache _info;

        static TypeSupportedChecker()
        {
            var warmedUp = TypeSupportedChecker.TryGetInfo(typeof(T));
            if(warmedUp!=null)
                _info = warmedUp;
            else
                _info = TypeSupportedChecker.WarmUpOne(typeof(T));
        }
    }
}