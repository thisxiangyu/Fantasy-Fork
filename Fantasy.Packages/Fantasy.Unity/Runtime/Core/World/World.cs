#pragma warning disable CS8603 // Possible null reference return.
#if FANTASY_NET
using Fantasy.Database;
using Fantasy.Platform.Net;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Fantasy
{
    /// <summary>
    /// 表示一个服务器【世界】
    /// </summary>
    public sealed class World : IDisposable
    {
        /// <summary>
        /// 获取世界的唯一标识。
        /// </summary>
        public uint Id { get; private init; }
        /// <summary>
        /// 世界名。
        /// </summary>
        public string Name { get; private init; }
        /// <summary>
        /// 本世界所有数据库, 按照工作职责号存取。
        /// </summary>
        public Dictionary<int, IDatabase> AllDatabases { get; init; }
        /// <summary>
        /// 本世界当前聚焦的数据库。
        /// </summary>
        public IDatabase? FocusedDatabase { get; set; }

        /// <summary>
        /// 根据工作职责号获取某个类型的数据库。
        /// </summary>
        /// <typeparam name="T">数据库类型</typeparam>
        /// <param name="duty">数据库工作职责号</param>
        /// <param name="shallSetAsFocused">是否同时设置为当前聚焦</param>
        /// <returns></returns>
        public T? GetDatabase<T>(int duty, bool shallSetAsFocused = true) where T : class, IDatabase
        {
            if(!AllDatabases.TryGetValue(duty, out IDatabase? database))
                return null;

            if(shallSetAsFocused)
                FocusedDatabase = database;

            var res = database as T;
            return res;
        }

        /// <summary>
        /// 重载方法, 根据工作职责号获取数据库接口。
        /// 拿到接口后, 可以有两种用法:
        /// 1. 强类型用法，转为对应的类型, 按照PostgreSQL、MongoDb等特定类型使用。
        /// 2. 弱类型用法，直接使用IDatabase接口, 调用其中 DbSession 内部的通用方法，适合简单CRUD。
        /// </summary>
        /// <param name="duty">数据库工作职责号</param>
        /// <param name="shallSetAsFocused">是否同时设置为当前聚焦</param>
        /// <returns></returns>
        public IDatabase? GetDatabase(int duty, bool shallSetAsFocused = true)
        {
            if (!AllDatabases.TryGetValue(duty, out IDatabase? database))
                return null;

            if (shallSetAsFocused)
                FocusedDatabase = database;

            var res = database;
            return res;
        }

        /// <summary>
        /// 获取游戏世界的配置信息。
        /// </summary>
        public WorldConfig Config => WorldConfigData.Instance.Get(Id);

        /// <summary>
        /// 使用指定的配置信息创建一个游戏世界实例。
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="worldConfigId"></param>
        private World(Scene scene, uint worldConfigId)
        {
            Id = worldConfigId;            
            Name = Config.WorldName;

            AllDatabases = InitDatabases(scene, Id);
        }

        public static Dictionary<int, IDatabase> InitDatabases(Scene scene, uint worldId) {

            var worldConfig = WorldConfigData.Instance.Get(worldId);
            Dictionary<int, IDatabase> allDatabases = new();

            if (worldConfig.DatabaseConfig == null)
                return allDatabases;

            for (int i = 0; i < worldConfig.DatabaseConfig.Length; i++)
            {
                var dbConfig = worldConfig.DatabaseConfig[i];

                if (string.IsNullOrWhiteSpace(dbConfig.DbConnection))
                    continue;

                string dbType = dbConfig.DbType.ToLower();
                switch (dbType)
                {
                    case "postgresql":
                    case "postgres":
                    case "pgsql":
                    case "pg":
                        {
                            if (allDatabases.ContainsKey(dbConfig.Duty))
                                throw new Exception($"CurrentDb {dbConfig.DbName} Duty({dbConfig.Duty}) is duplicated. Please verify your configuration.");

                            var pg = new PgSQL();
                            pg.Initialize(scene, dbConfig.Duty, dbConfig.DbConnection, dbConfig.DbName);
                            allDatabases.Add(dbConfig.Duty, pg);
                            break;
                        }
                    case "mongodb":
                    case "mongo":
                        {
                            if (allDatabases.ContainsKey(dbConfig.Duty))
                                throw new Exception($"CurrentDb {dbConfig.DbName} Duty({dbConfig.Duty}) is duplicated. Please verify your configuration.");

                            var mongo = new Mongo();
                            mongo.Initialize(scene, dbConfig.Duty, dbConfig.DbConnection, dbConfig.DbName);
                            allDatabases.Add(dbConfig.Duty, mongo);
                            break;
                        }
                    default:
                        {
                            throw new Exception("Not supported database type.");
                        }
                }
            }
            if (allDatabases.Count == 0)
            {
                Log.Warning($" Warning : Has no available database ! (World id:{worldId})(Scene configId:{scene.SceneConfigId})");
            }
            else
            {
                Log.Debug($"(World id:{worldId})(Scene configId:{scene.SceneConfigId}) Has successfully owned {allDatabases.Count} database(s): " +
                string.Join("，", allDatabases.Select((kv, index) =>
                $"{index + 1}.{kv.Value.GetDatabaseType} (Duty {kv.Key})")));   //依次打印世界中可用数据库的简要信息
            }

            return allDatabases;
        }

        /// <summary>
        /// 创建一个指定唯一标识的游戏世界实例。
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="id">游戏世界的唯一标识。</param>
        /// <returns>游戏世界实例。</returns>
        internal static World Create(Scene scene, uint id)
        {
            if (!WorldConfigData.Instance.TryGet(id, out var worldConfigData))
            {
                return null;
            }

            return (worldConfigData.DatabaseConfig == null || worldConfigData.DatabaseConfig.Length == 0)
            ? null
            : new World(scene, id);
        }

        /// <summary>
        /// 释放游戏世界资源。
        /// </summary>
        public void Dispose()
        {
            AllDatabases.Clear();
        }
    }
}

#endif