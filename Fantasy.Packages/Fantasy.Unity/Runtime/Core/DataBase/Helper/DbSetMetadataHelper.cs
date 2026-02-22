using System;
using System.Collections.Generic;
using System.Reflection;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.TypeMeta;

namespace Fantasy.Database.Helper
{
    /// <summary>
    /// DbSet类型元数据操作帮助类
    /// </summary>
    public static class DbSetMetadataHelper
    {
        /// <summary>
        /// 扫描程序集中所有含有[DbSet]标签的类型, 并进行操作。
        /// （初次调用这一方法将会自动为所有 DbSet 类型进行 WarmUp 从而解析主要信息。）
        /// <param name="doSomething">  传入针对性的操作函数。</param>
        /// </summary>       
        public static List<Type> ScanDbSetTypes(Action<Type, string, DbSetAttribute> doSomething)
        {
            List<Type> all = new();
            foreach (var kv in AssemblyManifest.Manifests)
            {
                var assm = kv.Value.Assembly;

#if DESIGN_TIME || UNITY_EDITOR
                Log.Info($"Scanning for DbSets in assembly: {assm.FullName}");
#endif

                foreach (var type in assm.GetTypes())
                {
                    var attr = type.GetCustomAttribute<DbSetAttribute>();

                    if (attr == null || type.IsAbstract)
                        continue;

                    // 检查标签中设置的 Name
                    string? tableName = default;
                    if (!string.IsNullOrWhiteSpace(attr.Name))
                        tableName = attr.Name;
                    else
                        tableName ??= $"{type.Name}"; // 没有用标签设置自定义表名, 那就直接用类名作为表名
                    all.Add(type);

                    if (doSomething != null)
                        doSomething.Invoke(type, tableName, attr);
                }
            }
            TypeDbSetChecker.WarmUp(all);
            return all;
        }
        /// <summary>
        /// 扫描程序集中所有含有[DbSet]标签的类型, 并进行操作。异步版本,传入异步函数。
        /// （初次调用这一方法将会自动为所有 DbSet 类型进行 WarmUp 从而解析主要信息。）
        /// </summary>
        public static async FTask<List<Type>> ScanDbSetTypesAsync(Func<Type, string, DbSetAttribute, FTask> doSomething)
        {
            List<Type> all = new();
            foreach (var kv in AssemblyManifest.Manifests)
            {
                var assm = kv.Value.Assembly;
#if DESIGN_TIME || UNITY_EDITOR
                Log.Info($"Scanning for DbSets in assembly: {assm.FullName}");
#endif
                foreach (var type in assm.GetTypes())
                {
                    var attr = type.GetCustomAttribute<DbSetAttribute>();

                    if (attr == null || type.IsAbstract)
                        continue;

                    // 检查标签中设置的 Name
                    string? tableName = default;
                    if (!string.IsNullOrWhiteSpace(attr.Name))
                        tableName = attr.Name;
                    else
                        tableName ??= $"{type.Name}"; // 没有用标签设置自定义表名, 那就直接用类名作为表名
                    all.Add(type);

                    if(doSomething != null)
                        await doSomething.Invoke(type, tableName, attr);
                }
            }
            TypeDbSetChecker.WarmUp(all);
            return all;
        }

        /// <summary>
        /// 获取非抽象类型DbSet标签 ；标签不存在或为抽象类型则返回Null 。
        /// </summary>
        public static DbSetAttribute? GetDbSetAttribute(Type type)
        {
            if (type.IsAbstract)
                return null;

            var attr = type.GetCustomAttribute<DbSetAttribute>();

            if (attr == null)
                return null;

            return attr;
        }
    }
}