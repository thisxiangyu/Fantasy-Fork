#if FANTASY_NET
using System.Linq.Expressions;
using Fantasy.Async;
using Fantasy.Database.Helper;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Database
{
    /// <summary>
    /// MongoDB的会话,适用于数据库基本CRUD操作
    /// </summary>
    public sealed partial class MongoSession : DbContext, IDbSession, IDisposable
    {
        private MongoDb mongo;

        /// <summary>
        /// 构造函数
        /// </summary>
        ///<param name="mongo"></param>
        public MongoSession(MongoDb mongo) {
            this.mongo = mongo;
        }

        /// <summary>
        /// 构造函数, 就这样继承就可以了 
        /// </summary>
        /// <param name="options"></param>
        public MongoSession(DbContextOptions options) : base(options)
        {

        }

        /// <summary>
        ///  " EntityTable" 映射模型构建阶段。
        ///  在 DbContext 首次实例化的时候会自动检查是否建构过模型，如果检测到从未建构过，OnModelCreating 就会生效。  
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            DbSetMetadataHelper.ScanDbSetTypes((type, tableName, attr) => {
                if (attr.IfSelectionContainsDbType(DatabaseType.MongoDB))
                {
                    Log.Debug($"MongoDb ORM-Model Registering entity: {type.FullName} -> table {tableName}");
                    modelBuilder.Entity(type).ToTable(tableName);
                }
            });

            // 剔除属性导航
            //modelBuilder.Model.GetEntityTypes()
            //    .Where(t => !t.ClrType.GetCustomAttributes(typeof(TableAttribute), true).Any() &&
            //                !t.ClrType.GetCustomAttributes(typeof(FantasyTableAttribute), true).Any())
            //    .ToList()
            //    .ForEachAllSingle(t => modelBuilder.Ignore(t.ClrType));

            //foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            //{
            //    Log.Warning($"EFCore 全部注册的实体: {entityType.ClrType.FullName}");
            //}

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public override void Dispose()
        {
            mongo = null;
            base.Dispose();
        }

        /// <summary>
        /// 异步销毁
        /// </summary>
        /// <returns></returns>
        public override async ValueTask DisposeAsync()
        {
            mongo = null;
            await FTask.CompletedTask;
            await base.DisposeAsync();
        }
          
        #region GetCollection

        /// <summary>
        /// 获取指定集合中的 MongoDB 文档的 IMongoCollection 对象。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="collection">集合名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>IMongoCollection 对象。</returns>
        public IMongoCollection<T> GetCollection<T>(string collection = null)
        {
            return mongo.RawHandler.GetCollection<T>(collection ?? typeof(T).Name);
        }

        /// <summary>
        /// 获取指定集合中的 MongoDB 文档的 IMongoCollection 对象，其中实体类型为 ToParentIs。
        /// </summary>
        /// <param name="name">集合名称。</param>
        /// <returns>IMongoCollection 对象。</returns>
        public IMongoCollection<Entity> GetCollection(string name)
        {
            return mongo.RawHandler.GetCollection<Entity>(name);
        }

        #endregion

        #region Count

        /// <summary>
        /// 快速估算指定集合中的文档数量。(非准确值, 但是很快)
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="collection">集合名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>文档数量。</returns>
        public async FTask<long> FastCount<T>(string collection = null) where T : Entity
        {
            return await GetCollection<T>(collection).EstimatedDocumentCountAsync();
        }

        /// <summary>
        /// 统计指定集合中的实体文档数量。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="collection">集合名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>实体文档数量。</returns>
        public async FTask<long> Count<T>(string collection = null) where T : Entity
        {
            return await GetCollection<T>(collection).CountDocumentsAsync(Builders<T>.Filter.Empty);
        }

        /// <summary>
        /// 统计指定集合中满足条件的文档数量。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="filter">用于筛选文档的表达式。</param>
        /// <param name="collection">集合名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>满足条件的文档数量。</returns>
        public async FTask<long> Count<T>(Expression<Func<T, bool>> filter, string collection = null) where T : Entity
        {
            return await GetCollection<T>(collection).CountDocumentsAsync(filter);
        }

        #endregion

        #region Exist

        /// <summary>
        /// 判断指定集合中是否存在文档。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="collection">集合名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>如果存在文档则返回 true，否则返回 false。</returns>
        public async FTask<bool> Exist<T>(string collection = null) where T : Entity
        {
            return await Count<T>(collection) > 0;
        }

        /// <summary>
        /// 判断指定集合中是否存在满足条件的文档。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="filter">用于筛选文档的表达式。</param>
        /// <param name="collection">集合名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>如果存在满足条件的文档则返回 true，否则返回 false。</returns>
        public async FTask<bool> Exist<T>(Expression<Func<T, bool>> filter, string collection = null) where T : Entity
        {
            return await Count(filter, collection) > 0;
        }

        #endregion

        #region Query

        /// <summary>
        /// 在不加数据库锁定的情况下，查询指定 ID 的文档。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="id">要查询的文档 ID。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>查询到的文档。</returns>
        public async FTask<T> QueryNotLock<T>(long id, bool isDeserialize = false, string collection = null) where T : Entity
        {
            var cursor = await GetCollection<T>(collection).FindAsync(d => d.Id == id);
            var v = await cursor.FirstOrDefaultAsync();

            if (isDeserialize && v != null)
            {
                v.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
            }

            return v;
        }

        /// <summary>
        /// 查询指定 ID 的文档，并加数据库锁定以确保数据一致性。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="id">要查询的文档 ID。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>查询到的文档。</returns>
        public async FTask<T> Query<T>(long id, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.Wait(id))
            {
                var cursor = await GetCollection<T>(collection).FindAsync(d => d.Id == id);
                var v = await cursor.FirstOrDefaultAsync();

                if (isDeserialize && v != null)
                {
                    v.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return v;
            }
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的文档数量和日期列表（不加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档数量和日期列表。</returns>
        public async FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, bool isDeserialize = false, string collection = null) where T : Entity
        {
            var count = await Count(filter);
            var dates = await QueryByPage(filter, pageIndex, pageSize, isDeserialize, collection);
            return ((int)count, dates);
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的文档数量和日期列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档数量和日期列表。</returns>
        public async FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                var count = await Count(filter);
                var dates = await QueryByPage(filter, pageIndex, pageSize, cols, isDeserialize, collection);
                return ((int)count, dates);
            }
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的文档列表（不加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档列表。</returns>
        public async FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                var list = await GetCollection<T>(collection).Find(filter).Skip((pageIndex - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的文档列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档列表。</returns>
        public async FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                var projection = Builders<T>.Projection.Include("");

                foreach (var col in cols)
                {
                    projection = projection.Include(col);
                }

                var list = await GetCollection<T>(collection).Find(filter).Project<T>(projection)
                    .Skip((pageIndex - 1) * pageSize).Limit(pageSize).ToListAsync();

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的文档列表，并按指定表达式进行排序（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="orderByExpression">排序表达式。</param>
        /// <param name="isAsc">是否升序排序。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档列表。</returns>
        public async FTask<List<T>> QueryByPageOrderBy<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression, bool isAsc = true, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                List<T> list;

                if (isAsc)
                {
                    list = await GetCollection<T>(collection).Find(filter).SortBy(orderByExpression).Skip((pageIndex - 1) * pageSize).Limit(pageSize).ToListAsync();
                }
                else
                {
                    list = await GetCollection<T>(collection).Find(filter).SortByDescending(orderByExpression).Skip((pageIndex - 1) * pageSize).Limit(pageSize).ToListAsync();
                }

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的第一个文档（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的第一个文档，如果未找到则为 null。</returns>
        public async FTask<T?> First<T>(Expression<Func<T, bool>> filter, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                var cursor = await GetCollection<T>(collection).FindAsync(filter);
                var t = await cursor.FirstOrDefaultAsync();

                if (isDeserialize && t != null)
                {
                    t.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return t;
            }
        }

        /// <summary>
        /// 通过指定 JSON 格式查询并返回满足条件的第一个文档（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的第一个文档。</returns>
        public async FTask<T> First<T>(string json, string[] cols, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                var projection = Builders<T>.Projection.Include("");

                foreach (var col in cols)
                {
                    projection = projection.Include(col);
                }

                var options = new FindOptions<T, T> { Projection = projection };

                FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);

                var cursor = await GetCollection<T>(collection).FindAsync(filterDefinition, options);
                var t = await cursor.FirstOrDefaultAsync();

                if (isDeserialize && t != null)
                {
                    t.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return t;
            }
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的一行。
        /// 如果超过一行会报错(说明逻辑错误或数据库中存储了不在预期内的额外数据)。
        /// </summary>
        /// <returns>如果未找到则为 null。</returns>
        public async FTask<T?> SingleOrDefault<T>(Expression<Func<T, bool>> filter, bool isDeserialize = true, string collection = null) where T : Entity
        {
            var res = await GetCollection<T>(collection)
                            .Find(filter)
                            .SingleOrDefaultAsync();
            if (isDeserialize)
                res.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
            return res;
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的文档列表，并按指定表达式进行排序（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="orderByExpression">排序表达式。</param>
        /// <param name="isAsc">是否升序排序。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档列表。</returns>
        public async FTask<List<T>> QueryOrderBy<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderByExpression, bool isAsc = true, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                List<T> list;

                if (isAsc)
                {
                    list = await GetCollection<T>(collection).Find(filter).SortBy(orderByExpression).ToListAsync();
                }
                else
                {
                    list = await GetCollection<T>(collection).Find(filter).SortByDescending(orderByExpression).ToListAsync();
                }

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的文档列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档列表。</returns>
        public async FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                var cursor = await GetCollection<T>(collection).FindAsync(filter);
                var list = await cursor.ToListAsync();

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        /// <summary>
        /// 根据指定 ID 加锁查询多个集合中的文档。
        /// </summary>
        /// <param name="id">文档 ID。</param>
        /// <param name="collectionNames">要查询的集合名称列表。</param>
        /// <param name="result">查询结果存储列表。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        public async FTask Query(long id, List<string>? collectionNames, List<Entity> result, bool isDeserialize = false)
        {
            using (await mongo.FlowLock.Wait(id))
            {
                if (collectionNames == null || collectionNames.Count == 0)
                {
                    return;
                }

                foreach (var collectionName in collectionNames)
                {
                    var cursor = await GetCollection(collectionName).FindAsync(d => d.Id == id);

                    var e = await cursor.FirstOrDefaultAsync();

                    if (e == null)
                    {
                        continue;
                    }

                    if (isDeserialize)
                    {
                        e.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                    }

                    result.Add(e);
                }
            }
        }

        /// <summary>
        /// 根据指定的 JSON 查询条件查询并返回满足条件的文档列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档列表。</returns>
        public async FTask<List<T>> QueryJson<T>(string json, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);
                var cursor = await GetCollection<T>(collection).FindAsync(filterDefinition);
                var list = await cursor.ToListAsync();

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        /// <summary>
        /// 根据指定的 JSON 查询条件查询并返回满足条件的文档列表，并选择指定的列（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档列表。</returns>
        public async FTask<List<T>> QueryJson<T>(string json, string[] cols, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                var projection = Builders<T>.Projection.Include("");

                foreach (var col in cols)
                {
                    projection = projection.Include(col);
                }

                var options = new FindOptions<T, T> { Projection = projection };

                FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);

                var cursor = await GetCollection<T>(collection).FindAsync(filterDefinition, options);
                var list = await cursor.ToListAsync();

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        /// <summary>
        /// 根据指定的 JSON 查询条件和任务 ID 查询并返回满足条件的文档列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="taskId">任务 ID。</param>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档列表。</returns>
        public async FTask<List<T>> QueryJson<T>(long taskId, string json, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.Wait(taskId))
            {
                FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);
                var cursor = await GetCollection<T>(collection).FindAsync(filterDefinition);
                var list = await cursor.ToListAsync();

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        /// <summary>
        /// 根据指定过滤条件查询并返回满足条件的文档列表，选择指定的列（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>满足条件的文档列表。</returns>
        public async FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, string[] cols, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                var projection = Builders<T>.Projection.Include("_id");

                foreach (var t in cols)
                {
                    projection = projection.Include(t);
                }

                var list = await GetCollection<T>(collection).Find(filter).Project<T>(projection).ToListAsync();

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        /// <summary>
        /// 根据指定过滤条件查询并返回满足条件的文档列表，选择指定的列（加锁）。
        /// </summary>
        /// <param name="filter">文档实体类型。</param>
        /// <param name="cols">查询过滤条件。</param>
        /// <param name="isDeserialize">要查询的列名称数组。</param>
        /// <param name="collection">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <typeparam name="T">集合名称。</typeparam>
        /// <returns></returns>
        public async FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>[] cols, bool isDeserialize = false, string collection = null) where T : Entity
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                var projection = Builders<T>.Projection.Include("_id");

                foreach (var col in cols)
                {
                    if (col.Body is not MemberExpression memberExpression)
                    {
                        throw new ArgumentException("Lambda expression must be a member access expression.");
                    }

                    projection = projection.Include(memberExpression.Member.Name);
                }

                var list = await GetCollection<T>(collection).Find(filter).Project<T>(projection).ToListAsync();

                if (!isDeserialize || list is not { Count: > 0 })
                {
                    return list;
                }

                foreach (var entity in list)
                {
                    entity.Deserialize(mongo.Scene, DeserializationRestore.FixEmbbeded);
                }

                return list;
            }
        }

        #endregion

        #region Save

        /// <summary>
        /// 保存实体对象到数据库（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="entity">要保存的实体对象。</param>
        /// <param name="collection">集合名称。</param>
        public async FTask Save<T>(object transactionSession, T? entity, string collection = null) where T : Entity
        {
            if (entity == null)
            {
                Log.Error($"save entity is null: {typeof(T).Name}");
                return;
            }

            var clone = mongo.Serializer.CloneEntity(entity);

            using (await mongo.FlowLock.Wait(clone.Id))
            {
                await GetCollection<T>(collection).ReplaceOneAsync(
                    (IClientSessionHandle)transactionSession, d => d.Id == clone.Id, clone,
                    new ReplaceOptions { IsUpsert = true });
            }
        }

        /// <summary>
        /// 保存实体对象到数据库（全量）。
        /// </summary>
        public async FTask Save<T>(T? entity, string collection = null) where T : Entity, new()
        {
            if (entity == null)
            {
                Log.Error($"save entity is null: {typeof(T).Name}");

                return;
            }

            var clone = mongo.Serializer.CloneEntity(entity);

            using (await mongo.FlowLock.Wait(clone.Id))
            {
                await GetCollection<T>(collection).ReplaceOneAsync(d => d.Id == clone.Id, clone, new ReplaceOptions { IsUpsert = true });
            }
        }

        /// <summary>
        /// 保存实体对象到数据库（部分字段）。
        /// </summary>
        public async FTask SavePartial<T>(T? entity,string collection,params string[] propertyNames) where T : Entity, new()
        {
            if (entity == null)
            {
                Log.Error($"Entity is null: {entity.GetType()}");
                return;
            }

            if (propertyNames == null || propertyNames.Length == 0)
                return;

            using (await mongo.FlowLock.Wait(entity.Id))
            {
                var updates = new List<UpdateDefinition<T>>();

                var type = typeof(T);

                foreach (var name in propertyNames)
                {
                    var prop = type.GetProperty(name);
                    if (prop == null)
                        throw new InvalidOperationException(
                            $"Property '{name}' not found on {type.Name}");

                    var value = prop.GetValue(entity);

                    updates.Add(Builders<T>.Update.Set(name, value));
                }

                var combinedUpdate = Builders<T>.Update.Combine(updates);

                await GetCollection<T>(collection).UpdateOneAsync(
                    x => x.Id == entity.Id,
                    combinedUpdate,
                    new UpdateOptions { IsUpsert = false });
            }
        }

        /// <summary>
        /// 保存实体对象到数据库（加锁）。
        /// </summary>
        /// <param name="filter">保存的条件表达式。</param>
        /// <param name="entity">实体类型。</param>
        /// <param name="collection">集合名称。</param>
        /// <typeparam name="T"></typeparam>
        public async FTask Save<T>(Expression<Func<T, bool>> filter, T? entity, string collection = null) where T : Entity, new()
        {
            if (entity == null)
            {
                Log.Error($"save entity is null: {typeof(T).Name}");
                return;
            }

            T clone = mongo.Serializer.CloneEntity(entity);

            using (await mongo.FlowLock.Wait(clone.Id))
            {
                await GetCollection<T>(collection).ReplaceOneAsync<T>(filter, clone, new ReplaceOptions { IsUpsert = true });
            }
        }

        /// <summary>
        /// 保存多个实体对象到数据库（加锁）。
        /// </summary>
        /// <param name="id">文档 ID。</param>
        /// <param name="entities">要保存的实体对象列表。</param>
        public async FTask Save(long id, List<Entity>? entities)
        {
            if (entities == null || entities.Count == 0)
            {
                Log.Error("save entity is null");
                return;
            }

            using var listPool = ListPool<Entity>.Create();

            foreach (var entity in entities)
            {
                listPool.Add(mongo.Serializer.CloneEntity(entity));
            }

            using (await mongo.FlowLock.Wait(id))
            {
                foreach (var clone in listPool)
                {
                    try
                    {
                        await GetCollection(clone.GetType().Name).ReplaceOneAsync(d => d.Id == clone.Id, clone, new ReplaceOptions { IsUpsert = true });
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Save Archetypes ToParentIs Error: {clone.GetType().Name} {clone}\n{e}");
                    }
                }
            }
        }

        #endregion

        #region Insert

        /// <summary>
        /// 插入单个实体对象到数据库（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="entity">要插入的实体对象。</param>
        /// <param name="collection">集合名称。</param>
        /// <param name="transaction">事务。</param>
        public async FTask Insert<T>(T? entity, string collection = null, object? transaction = null) where T : Entity, new()
        {
            if (entity == null)
            {
                Log.Error($"insert entity is null: {typeof(T).Name}");
                return;
            }

            var clone = mongo.Serializer.CloneEntity(entity);

            using (await mongo.FlowLock.Wait(entity.Id))
            {
                if (transaction == null)
                    await GetCollection<T>(collection).InsertOneAsync(clone);
                else
                    await GetCollection<T>(collection).InsertOneAsync((IClientSessionHandle)transaction, clone);
            }
        }

        /// <summary>
        /// 批量插入实体对象列表到数据库（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="list">要插入的实体对象列表。</param>
        /// <param name="collection">集合名称。</param>
        /// <param name="transaction">事务。</param>
        public async FTask InsertBatch<T>(IEnumerable<T> list, string collection = null, object? transaction = null) where T : Entity, new()
        {
            using (await mongo.FlowLock.WaitIfTooMuch())
            {
                if(transaction==null)
                    await GetCollection<T>(collection).InsertManyAsync(list);
                else
                    await GetCollection<T>(collection).InsertManyAsync((IClientSessionHandle)transaction, list);
            }
        }

        /// <summary>
        /// 插入BsonDocument到数据库（加锁）。
        /// </summary>
        /// <param name="bsonDocument"></param>
        /// <param name="taskId"></param>
        /// <typeparam name="T"></typeparam>
        public async Task Insert<T>(BsonDocument bsonDocument, long taskId) where T : Entity
        {
            using (await mongo.FlowLock.Wait(taskId))
            {
                await GetCollection<BsonDocument>(typeof(T).Name).InsertOneAsync(bsonDocument);
            }
        }

        #endregion

        #region Remove

        /// <summary>
        /// 根据ID删除单个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="id">要删除的实体的ID。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(object transactionSession, long id, string collection = null)
            where T : Entity, new()
        {
            using (await mongo.FlowLock.Wait(id))
            {
                var result = await GetCollection<T>(collection)
                    .DeleteOneAsync((IClientSessionHandle)transactionSession, d => d.Id == id);
                return result.DeletedCount;
            }
        }

        /// <summary>
        /// 根据ID删除单个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="id">要删除的实体的ID。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(long id, string collection = null) where T : Entity, new()
        {
            using (await mongo.FlowLock.Wait(id))
            {
                var result = await GetCollection<T>(collection).DeleteOneAsync(d => d.Id == id);
                return result.DeletedCount;
            }
        }

        /// <summary>
        /// 根据ID和筛选条件删除多个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="coroutineLockQueueKey">异步锁Id。</param>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="filter">筛选条件。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(long coroutineLockQueueKey, object transactionSession,
            Expression<Func<T, bool>> filter, string collection = null) where T : Entity, new()
        {
            using (await mongo.FlowLock.Wait(coroutineLockQueueKey))
            {
                var result = await GetCollection<T>(collection)
                    .DeleteManyAsync((IClientSessionHandle)transactionSession, filter);
                return result.DeletedCount;
            }
        }

        /// <summary>
        /// 根据ID和筛选条件删除多个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="coroutineLockQueueKey">异步锁Id。</param>
        /// <param name="filter">筛选条件。</param>
        /// <param name="collection">集合名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(long coroutineLockQueueKey, Expression<Func<T, bool>> filter,
            string collection = null) where T : Entity, new()
        {
            using (await mongo.FlowLock.Wait(coroutineLockQueueKey))
            {
                var result = await GetCollection<T>(collection).DeleteManyAsync(filter);
                return result.DeletedCount;
            }
        }

        #endregion       

        #region Utility

        /// <summary>
        /// 对满足条件的文档中的某个数值字段进行求和操作。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="filter">用于筛选文档的表达式。</param>
        /// <param name="sumExpression">要对其进行求和的字段表达式。</param>
        /// <param name="collection">集合名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>满足条件的文档中指定字段的求和结果。</returns>
        public async FTask<long> Sum<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> sumExpression, string collection = null) where T : Entity
        {
            var member = (MemberExpression)((UnaryExpression)sumExpression.Body).Operand;
            var projection = new BsonDocument("_id", "null").Add("Result", new BsonDocument("$sum", $"${member.Member.Name}"));
            var data = await GetCollection<T>(collection).Aggregate().Match(filter).Group(projection).FirstOrDefaultAsync();
            return data == null ? 0 : Convert.ToInt64(data["Result"]);
        }

        #endregion

        #region Detach, MongoDB不需要这些方法, 仅占位实现
        public void ClearTracker()
        {
            
        }

        public void Detach(Entity entity, bool cascade)
        {

        }

        public void DetachExisting<T>(T entity) where T : Entity
        { 
        
        }
        #endregion
        }
}
#endif