using System;
using System.Collections.Generic;
using Fantasy.Database;
using Fantasy.Database.Attributes;
using Fantasy.Database.Helper;
using MemoryPack;
using Fantasy.DataStructure.Dictionary;

namespace Fantasy.Entitas.TypeMeta
{
    /// <summary>
    /// 类型DbSet特性静态信息缓存类
    /// </summary>
    public static class TypeDbSetChecker
    {
        /// <summary>
        /// 类型支持特性的缓存信息
        /// </summary>
        public static Int64FrozenDictionary<TypeDbSetCache>? InfoByHash;

        /// <summary>
        /// 预热，执行一次，就会缓存所有检查结果。
        /// 优先使用源生成 (Source Generator) 的 IDbSetModelBuilderRegistrar 获取缓存，
        /// 替代运行时的反射扫描 (ScanDbSetTypes)。
        /// </summary>
        public static void WarmUp(IEnumerable<Type> types)
        {
            if (InfoByHash != null)
                return;
            WarmUpFromSourceGen();
        }

        /// <summary>
        /// 重新预热
        /// </summary>
        public static void ReWarmUp(IEnumerable<Type> types)
        {
            WarmUpFromSourceGen();
        }

        /// <summary>
        /// 从源生成器生成的 IDbSetModelBuilderRegistrar 中获取缓存字典。
        /// 替代原来的反射遍历构建字典。
        /// </summary>
        private static void WarmUpFromSourceGen()
        {
            var mergedDict = new Dictionary<long, TypeDbSetCache>();

            foreach (var (_, assemblyManifest) in Assembly.AssemblyManifest.Manifests)
            {
                if (assemblyManifest.DbSetModelBuilderRegistrar != null)
                {
                    var cache = assemblyManifest.DbSetModelBuilderRegistrar.GetDbSetTypeCache();
                    if (cache != null)
                    {
                        foreach (var kv in cache)
                        {
                            // 规范嵌入标记
                            if (kv.Value.DbSetAttri != null && kv.Value.DbSetAttri.IsEmbedded)
                                kv.Value.DbSetAttri.IsAsDocument = true;

                            mergedDict[kv.Key] = kv.Value;
                        }
                    }
                }
            }

            if (mergedDict.Count == 0)
                mergedDict.Add(-1, new TypeDbSetCache());

            InfoByHash = new Int64FrozenDictionary<TypeDbSetCache>(mergedDict);
        }

        /// <summary>
        /// 获取类型的支持特性信息 ( 需要先确保已经预热读取过 )
        /// </summary>
        public static TypeDbSetCache? GetWarmInfo(Type type)
        {
            if (InfoByHash == null)
                WarmUpFromSourceGen();

            if (InfoByHash == null || !InfoByHash.TryGetValue(TypeHashCache.GetHashCode(type), out var info))
                return null;

            return info;
        }
    }

    /// <summary>
    /// 类型DbSet信息缓存。
    /// </summary>
    public class TypeDbSetCache
    {
        /// <summary>
        /// 获取实体类型的标签[DbSet], 如果不存在则为null。
        /// </summary>
        public DbSetAttribute? DbSetAttri { get; internal set; }

        /// <summary>
        /// 无参构造函数
        /// </summary>
        public TypeDbSetCache() { }

        /// <summary>
        /// 通过 DbSetAttribute 构造（供源生成代码使用）
        /// </summary>
        public TypeDbSetCache(DbSetAttribute? dbSetAttri)
        {
            DbSetAttri = dbSetAttri;
        }
        /// <summary>
        /// 返回是否是嵌入式存储。
        /// </summary>
        /// <returns></returns>
        public bool IsEmbedded()
        {
            if (DbSetAttri == null)
                return false;

            if (DbSetAttri.IsEmbedded)
                DbSetAttri.IsAsDocument = true;

            return DbSetAttri.IsEmbedded;
        }
        /// <summary>
        /// 是否是文档式存储。
        /// </summary>
        /// <returns></returns>
        public bool IsAsDoc()
        {
            if (DbSetAttri == null)
                return false;

            if (DbSetAttri.IsEmbedded)
                DbSetAttri.IsAsDocument = true;

            return DbSetAttri.IsAsDocument;
        }
    }

    /// <summary>
    /// 类型支持特性检查器。泛型检查版本。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class TypeDbSetChecker<T> where T : Entity
    {
        /// <summary>
        /// 是否有DbSet标签
        /// </summary>
        public static bool IsDbSet => _isDbSet;
        private static readonly bool _isDbSet;
        /// <summary>
        /// 是否以嵌入式存储
        /// </summary>
        public static bool IsEmbedded => _isEmbedded;
        private static readonly bool _isEmbedded;
        /// <summary>
        /// 是否以文档式存储
        /// </summary>
        public static bool IsAsDoc => _isAsDoc;
        private static readonly bool _isAsDoc;
        /// <summary>
        /// 是否以二进制字节存储
        /// </summary>
        public static bool IsAsBytes => _isAsBytes;
        private static readonly bool _isAsBytes;
        /// <summary>
        /// 是否带命名空间
        /// </summary>
        public static bool IsWithNamespace => _isWithNamespace;
        private static readonly bool _isWithNamespace;
        /// <summary>
        /// 获取DbSetName
        /// </summary>
        public static string DbSetName => _dbSetName;
        private static readonly string _dbSetName;
        /// <summary>
        /// 获取DocName(如果存在)
        /// </summary>
        public static string? DocName => _docName;
        private static readonly string? _docName;
        /// <summary>
        /// 获取影子实体名(如果存在)。
        /// <para>
        /// 影子实体是EFCore为 SharedTypeEntity(共享类型实体) 建模时产生的, 通常为<see cref="Dictionary{TString, TObject}"/>类型。
        /// 通过 ShadowName 才能区分具体类型.
        /// </para>
        /// </summary>
        public static string? ShadowName => _shadowName;
        private static readonly string? _shadowName;

        private static readonly TypeDbSetCache? _info;

        static TypeDbSetChecker()
        {
            // ——  只计算一次 ——

            _info = TypeDbSetChecker.GetWarmInfo(typeof(T));
            if (_info == null)  //没有DbSet标签
            {
                _isDbSet = false;
                _isEmbedded = false;
                _isAsDoc = false;
                _isAsBytes = false;
                _isWithNamespace = false;
                _dbSetName = string.Empty;
                _docName = null;
                _shadowName = null;
                return;
            }

            _isDbSet = _info.DbSetAttri != null;
            _isEmbedded = _info.IsEmbedded();
            _isAsDoc = _info.IsAsDoc();
            _isAsBytes = _info.DbSetAttri?.IsAsBytes ?? false;
            _isWithNamespace = _info.DbSetAttri?.WithNamespace ?? false;

            var attr = _info.DbSetAttri;
            if (attr == null)
            {
                _dbSetName = string.Empty;
            }
            else
            {
                string? name = attr.Name;
                _dbSetName = string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name;
            }

            if (_isAsDoc)
            {
                string? nameSpace = typeof(T).Namespace;
                if (IsWithNamespace && nameSpace != null)
                    _docName = $"{nameSpace}.{_dbSetName}_Doc";
                else
                    _docName = $"{_dbSetName}_Doc";

                _shadowName = $"{typeof(T).FullName}_Shadow";
            }
        }
    }
}