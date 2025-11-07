using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Entitas.TypeMeta;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy.Database
{
    /// <summary>
    /// DbSet类型元数据操作帮助类
    /// </summary>
    internal static class DbSetMetadataHelper
    {
        /// <summary>
        /// 扫描程序集中所有含有[DbSet]标签的实体类型, 并进行操作。
        /// （初次调用这一方法将会自动为所有 DbSet 类型进行 WarmUp 从而解析主要信息。）
        /// <param name="doSomething">  传入针对性的操作函数。</param>
        /// </summary>       
        public static List<Type> ScanDbSetEntityTypes(Action<Type, string, DbSetAttribute> doSomething) {
            List <Type> all = new ();
            foreach (var kv in AssemblyManifest.Manifests)
            {
                var assm = kv.Value.Assembly;

                //Log._info($"Scanning for FantasyDbSets in assembly: {assm.FullName}");

                foreach (var type in assm.GetTypes())
                {
                    var attr = type.GetCustomAttribute<DbSetAttribute>();

                    if (attr == null || type.IsAbstract || !type.IsSubclassOf(typeof(Entity)))
                        continue;

                    // 检查标签中设置的 Name
                    string? tableName = default;
                    if (!string.IsNullOrWhiteSpace(attr.Name))
                        tableName = attr.Name;
                    else
                        tableName ??= $"{type.Name}"; // 没有用标签设置自定义表名, 那就直接用类名作为表名
                    all.Add(type);
                    doSomething.Invoke(type, tableName, attr);
                }
            }
            TypeDbSetInfos.WarmUp(all);
            return all;
        }
        /// <summary>
        /// 扫描程序集中所有含有[DbSet]标签的类型, 并进行操作。异步版本,传入异步函数。
        /// （初次调用这一方法将会自动为所有 DbSet 类型进行 WarmUp 从而解析主要信息。）
        /// </summary>
        public static async FTask<List<Type>> ScanDbSetEntityTypesAsync(Func<Type, string, DbSetAttribute, FTask> doSomething)
        {
            List<Type> all = new();
            foreach (var kv in AssemblyManifest.Manifests)
            {
                var assm = kv.Value.Assembly;

                Log.Info($"Scanning assembly: {assm.FullName}");
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
                    await doSomething.Invoke(type, tableName, attr);
                }
            }
            TypeDbSetInfos.WarmUp(all);
            return all;
        }

        /// <summary>
        /// 获取实体类型实现了外键接口的所有父级类型
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static HashSet<Type> GetForeignKeyParentTypes(Type entityType)
        {
            HashSet<Type> parentTypes = new ();

            foreach (var iface in entityType.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                var genericDef = iface.GetGenericTypeDefinition();
                if (genericDef == typeof(IDbSetRef<>)
                    || genericDef == typeof(IDbSetRef<,>)
                    || genericDef == typeof(IDbSetRef<,,>))
                {
                    foreach (var t in iface.GetGenericArguments())
                    {
                        parentTypes.Add(t); 
                    }
                }
            }
            return parentTypes;
        }

        /// <summary>
        /// 获取非抽象类型DbSet标签 ；标签不存在或为抽象类型则返回Null 。
        /// </summary>
        /// <returns></returns>
        public static DbSetAttribute? GetDbSetAttribute(Type type) {
            var attr = type.GetCustomAttribute<DbSetAttribute>();

            if (attr == null || type.IsAbstract)
                return null;

            return attr;
        }
    }
}
