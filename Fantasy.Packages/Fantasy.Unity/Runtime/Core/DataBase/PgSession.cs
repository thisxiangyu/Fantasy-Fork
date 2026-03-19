#if FANTASY_NET
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Dapper;
using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Database.Attributes;
using Fantasy.Database.DataTransfer;
using Fantasy.Database.Helper;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
using Fantasy.Entitas.TypeMeta;
using Fantasy.Helper;
using Fantasy.Pool;
using Fantasy.Product;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Bson;
using MongoDB.Driver;
using Npgsql;
using static Fantasy.Helper.JsonHelper;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Database
{
    /// <summary>
    /// 选择操作SQL数据库时用哪个ORM。 Dapper略快但会牺牲高级特性。
    /// </summary>
    public enum PreferSqlMode
    {
        /// <summary>
        /// 更青睐用EFCore
        /// </summary>
        EFCore,
        /// <summary>
        /// 更青睐用Dapper
        /// </summary>
        Dapper
    }

    /// <summary>
    /// 用于给<see cref="PgSession"/>注册为非池化的版本
    /// </summary>    
    public class PgSessionUnPooled : PgSession
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="options"></param>
        public PgSessionUnPooled(DbContextOptions<PgSessionUnPooled> options) : base(options)
        {
        }
    }

    /// <summary>
    /// 配置表数据库专用会话
    /// </summary>
    public class PgSessionForConfig : PgSession
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="options"></param>
        public PgSessionForConfig(DbContextOptions<PgSessionForConfig> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Design(modelBuilder, isSessionForConfig: true);
        }
    }

    /// <summary>
    /// 配置表数据库专用会话的非池化版本
    /// </summary>
    public class PgSessionUnPooledForConfig : PgSession
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="options"></param>
        public PgSessionUnPooledForConfig(DbContextOptions<PgSessionUnPooledForConfig> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Design(modelBuilder, isSessionForConfig: true);
        }
    }

    /// <summary>
    /// <para>
    /// 【<see cref="PgSession"/>是 PgSql 数据库的操作会话, 继承自EFCore 的 DbContext。适用于PgSQL的 CRUD 操作。】
    /// </para>
    /// <para>
    /// 【注意, 每个 <see cref="PgSession"/> 会交由EFCore构建一个“Entity - Table”模型】
    ///  模型初始化时自动构建, 构建后无法运行时变动! 无法HotUpdate！无法检测到动态新建的表、无法感知热更新建的字段！ 
    ///  因此，对于已上线的业务，如果需要无感更新业务数据, (典型的情况是中途给实体新增字段 )，
    ///  请务必考虑采用【蓝绿部署】结合"数据库迁移策略"；
    ///  另一种可能有效的运行时让服务无感交接的解决办法是：继承并实现额外的DbContext，比如实现一组临时的 TempSession 与 TempSessionUnPooled ，专门用于“EntityTable”模型的热交接（但即便如此, 等到合适的时机依然需要重启服务器，构建新的模型）。
    /// </para>
    /// <para>
    /// 【关于 <see cref="PgSession"/> 的池化机制】 由ServiceProvider依赖注入进行池化，
    /// 具体详情请见 PostgreSQL 初始化时调用的 ServiceCollection.AddDbContextPool() 方法，
    /// 微软在该方法中对 DbContext 的池化做了详细注释。
    /// </para>
    /// <para>
    /// 【关于 ORM 性能】<see cref="PgSession"/> 基于 Dapper和EFCore 封装。
    /// 从性能来考虑, 4种 SQL 操作方式排名如下——
    /// 1. 原生SQL: 性能最优, 但没有 ORM 字段自动映射; 
    /// 2. Dapper: 相当于原生SQL + 自动字段映射, 性能次之;
    /// 3. EFCore: 如果使用 FromSqlRaw , 性能可能与 Dapper 相当;
    /// 4. EFCore一般情况: 适合多人合作开发, 提供高级特性, 如导航属性、LINQ 、Change Tracking 、事务管理。
    /// 当前封装，结合了1 2 3 4，以寻求均衡。
    /// 为什么当前不用号称速度更快的 ORM 框架，比如SQLSugar ？考虑到，现在AI时代, 复杂查询可以让AI生成最优的原生SQL执行, ORM 的性能不必须作为核心考虑项。  
    /// 而且许多数据库的高级用法，更常发生在响应非实时的后运营阶段、离线处理阶段，这时SQL执行效率并不追求极限，EFCore完全足以胜任绝大多数状况。
    /// </para>
    /// <para>
    /// 非池中取得的<see cref="PgSession"/>, 劣势: 每次使用都会产生新实例, 相比池化, 会施加轻微GC压力。优势: 绝对的状态安全。
    /// 调用 PostgreSQL.Use() 时，传入参数设置未非池化， 即可获取 <see cref="PgSessionUnPooled"/> 实例。
    /// </para>
    /// </summary>
    public partial class PgSession : DbContext, IDbSession, IAssemblyLifecycle
    {
        /// <summary>
        /// Pg的实例引用
        /// </summary>
        private PostgreSQL pg;
        /// <summary>
        /// 设置Pg实例引用
        /// </summary>
        /// <param name="Pg"></param>
        public void SetPg(PostgreSQL Pg) {
            pg = Pg;
        }

        /// <summary>
        /// 构造函数, 就这样继承就可以了 
        /// </summary>
        /// <param name="options"></param>
        public PgSession(DbContextOptions options) : base(options)
        {

        }

        private JsonSettings _jsonSettings = new JsonSettings(
                library: Library.Microsoft, 
                isIndented: false,
                writeTypeWhenNecessary: true);

        /// <summary>
        ///  " EntityTable" 映射模型构建阶段。
        ///  在 DbContext 首次实例化的时候会自动检查是否建构过模型，如果检测到从未建构过，OnModelCreating 就会生效。  
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Design(modelBuilder, isSessionForConfig: false);
        }

        /// <summary>
        /// 设计模型。
        /// </summary>
        /// <param name="modelBuilder">传入一个建模器</param>
        /// <param name="isSessionForConfig">是否作为配置表数据库专用会话</param>
        protected void Design(ModelBuilder modelBuilder, bool isSessionForConfig) {
            //处理DbSet注册
            DbSetMetadataHelper.ScanDbSetTypes((type, tableName, attr) => {

                if (!attr.IfSelectionContainsDbType(DatabaseType.PostgreSQL))
                    return;

                if (attr.IsEmbedded)
                {
                    Log.Info($"{type} is set as Embedded, been ignoured in PgSQL ORM-Model.");
                    return;
                }

                // 设置默认 schema
                const string defaultSchema = "default";
                modelBuilder.HasDefaultSchema(defaultSchema);

                string schemaStr = defaultSchema;//PgSQL默认命名空间
                if (attr.WithNamespace == true && !string.IsNullOrWhiteSpace(type.Namespace))
                    schemaStr = type.Namespace.ReplaceDotWith_();

                // 数据库分为两种，过滤规则如下：
                // 如果当前数据库没有被设置为配置表数据库, 那就不处理配置表类型的DbSet；
                // 如果当前数据库被设置为配置表数据库, 那就不处理那些普通的DbSet。
                if (attr.IsAsConfig == true)
                {
                    PgSQL.ExistingAtLeastOneConfigDbSet = true;

                    if (!isSessionForConfig)
                        return; //直接返回

                    if (schemaStr == defaultSchema)
                        schemaStr = "Config";
                    else
                        schemaStr = "Config_" + schemaStr;
                }
                else if(!attr.IsAsConfig && isSessionForConfig)
                {
                    return; //直接返回
                }

                Log.Debug($"PgSQL ORM-Model Registering entities: {type.FullName} -> table {schemaStr}.{tableName}");

                EntityTypeBuilder? entityBuilder = default;
                if (attr.IsAsDocument)
                {
                    //文档建表 (通过 SharedTypeEntity, 这段逻辑会在EF模型中创建一个模型内共享类型实体 )
                    if (typeof(Entity).IsAssignableFrom(type))
                        entityBuilder = modelBuilder.SharedTypeEntity<EntityDocumentDTC>($"{type.FullName}_Shadow");
                    else
                        entityBuilder = modelBuilder.SharedTypeEntity<DocumentDTC>($"{type.FullName}_Shadow");

                    entityBuilder.ToTable($"{tableName}_Doc", schemaStr);

                    if (typeof(Entity).IsAssignableFrom(type)) 
                    {
                        entityBuilder.Property<long>("Id").ValueGeneratedNever();
                    }
                    else// 非Entity对象
                    {
                        entityBuilder.Property<long>("Id").UseIdentityColumn(); // 数据库自增
                    }

                    entityBuilder.Property<object?>(DbSetProperty.DocAsJson).HasColumnType("jsonb").IsRequired(false);
                    //Note : 暂不支持, 因为byte[]在EFCore中不知道能否池化
                    //entityBuilder.Property<byte[]?>(DbSetProperty.DocAsBytes).HasColumnType("bytea").IsRequired(false);
                }
                else
                {
                    //实体建表
                    entityBuilder = modelBuilder.Entity(type).ToTable(tableName, schemaStr);

                    if (typeof(Entity).IsAssignableFrom(type))
                    {
                        //承载Embedded实体的影子属性, 自定义序列化转化逻辑
                        entityBuilder.Property<ReuseList<Entity>>(nameof(Entity.EmbbededSingle)).HasColumnType("jsonb")
                            .HasColumnName(DbSetProperty.JsonSingle)
                            .HasConversion(
                                           entityList => entityList.ToJson(_jsonSettings, true),
                                           jsonStr => jsonStr.Deserialize<ReuseList<Entity>>(_jsonSettings,DetectMode.MustBeWrapper,true)
                                           )
                            .IsRequired(false);
                        entityBuilder.Property<ReuseList<Entity>>(nameof(Entity.EmbbededMulti)).HasColumnType("jsonb")
                            .HasColumnName(DbSetProperty.JsonMulti)
                            .HasConversion(
                                           entityList => entityList.ToJson(_jsonSettings, true),
                                           jsonStr => jsonStr.Deserialize<ReuseList<Entity>>(_jsonSettings, DetectMode.MustBeWrapper,true)
                                           )
                            .IsRequired(false);
                        //Note : 暂不支持, 因为byte[]在EFCore中不知道能否池化
                        //entityBuilder.Property<byte[]>(DbSetProperty.BytesSingle).HasColumnType("bytea")
                        //    .IsRequired(false);
                        //entityBuilder.Property<byte[]>(DbSetProperty.BytesMulti).HasColumnType("bytea")
                        //    .IsRequired(false);
                    }
                }

                if (typeof(Entity).IsAssignableFrom(type))
                {
                    //父级Type+Id联合索引
                    entityBuilder.Property<long>(DbSetProperty.ParentType);
                    entityBuilder.Property<long>(DbSetProperty.ParentId);
                    entityBuilder.HasIndex(DbSetProperty.ParentType, DbSetProperty.ParentId).IsUnique(false);
                }
            });

            if (!modelBuilder.Model.GetEntityTypes().Any())
            {
                Log.Warning($"❌ No entities were detected during the EF Core model-building phase. Please verify! isSessionForConfig : {isSessionForConfig}");
            }

            // 进行一次GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyManifest"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async FTask OnLoad(AssemblyManifest assemblyManifest)
        {
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assemblyManifest"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>

        public async FTask OnUnload(AssemblyManifest assemblyManifest)
        {
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public override void Dispose()
        {
            Mode = PreferSqlMode.EFCore;
            pg = null;
            base.Dispose();
        }

        /// <summary>
        /// 异步释放
        /// </summary>
        public override async ValueTask DisposeAsync()
        {
            Mode = PreferSqlMode.EFCore;
            pg = null;
            await base.DisposeAsync();
        }

        /// <summary>
        /// Sql 操作倾向模式选择, 默认选择是 EFCore, 倾向于使用 EFCore。
        /// !! 需特别注意, 调用属于 IDbSession接口中的查询方法时， 使用Dapper模式, 会有一定的性能提升, 但这样就导致默认不会被 EFCore 框架自动跟踪属性变化了 !!
        /// </summary>
        public PreferSqlMode Mode { get; set; } = PreferSqlMode.EFCore;

        #region Table

        /// <summary>
        /// PgSQL中表名即类型名, 如果通过 OnModelCreating 配置了指定表名, 才需用自定义名。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <returns>表名。</returns>
        private string GetTableName<T>(string table = null) where T : Entity
        {
            return table ?? typeof(T).Name;
        }

        /// <summary>
        /// 从表达式中获取列名。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="expression">字段表达式。</param>
        /// <returns>列名。</returns>
        private string GetColumnName<T>(Expression<Func<T, object>> expression)
        {
            // if (expression.Body is MemberExpression memberExpression)
            // {
            //     return memberExpression.Member.Name;
            // }
            // else if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression)
            // {
            //     return ((MemberExpression)unaryExpression.Operand).Member.Name;
            // }
            return string.Empty;
        }

        /// <summary>
        /// 将LINQ表达式转换为SQL WHERE子句。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="expression">LINQ表达式。</param>
        /// <returns>SQL WHERE子句。</returns>
        private string GetWhereClause<T>(Expression<Func<T, bool>> expression)
        {
            // 这里应该实现LINQ表达式到SQL的转换
            // 为简化，返回一个占位符
            return "1=1";
        }

        #endregion

        #region Count

        ///// <summary>
        ///// 统计指定表中的总行数。
        ///// </summary>
        ///// <typeparam name="P">实体类型。</typeparam>
        ///// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        ///// <param name="dbContext">上下文，可选。</param>
        ///// <returns>总行数。</returns>
        //public async FTask<long> Count<P>(string table = null,DbContext dbContext = null) where P : ToParentIs
        //{
        //    var tableName = GetTableName<P>(table);
        //    var connection = Handler.CreateConnection();
        //    try
        //    {
        //        await connection.OpenAsync(); 
        //        using (var cmd = connection.CreateCommand())
        //        {
        //            tableName = tableName.Replace("\"", "\"\"");
        //            cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";
        //            var entities = await cmd.ExecuteScalarAsync();
        //            return Convert.ToInt64(entities);
        //        }
        //    }
        //    finally
        //    {
        //        await connection.CloseAsync();
        //    }
        //}

        /// <summary>
        /// 自行执行一句原生SQL语句
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        private async FTask<object?> ExecuteRawSqlOnceDirectly(string sql) {

            var connection = await GetOpenedConnection();//确保开启连接
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return await cmd.ExecuteScalarAsync();
        }

        /// <summary>
        /// 获取数据库连接, 并确保连接已打开。
        /// </summary>
        /// <returns></returns>
        public async FTask<DbConnection> GetOpenedConnection() {
            var _connection = Database.GetDbConnection();
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();
            return _connection;
        }

        /// <summary>
        /// 统计指定表中的总行数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>总行数。</returns>
        public async FTask<long> Count<T>(string table = null) where T : Entity
        {
            var tableName = GetTableName<T>(table);                     

            string sql = $"SELECT COUNT(*) FROM \"{tableName}\"";

            switch (Mode)
            {
                case PreferSqlMode.EFCore:
                    {
                        return await Database.SqlQueryRaw<long>(sql).FirstOrDefaultAsync();
                    }
                default:
                    {
                        var result = ExecuteRawSqlOnceDirectly(sql);
                        return Convert.ToInt64(result);
                    }
            }
        }

        /// <summary>
        /// 统计指定表中满足条件的行数量。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="filter">用于筛选行的条件。</param>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>满足条件的行数量。</returns>
        public async FTask<long> Count<T>(Expression<Func<T, bool>> filter, string table = null) where T : Entity
        {
            return await Set<T>().CountAsync(filter);
        }

        /// <summary>
        /// 快速估算表行数, 适合估算大表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public async FTask<long> FastCount<T>(string table = null) where T : Entity
        {
            var tableName = GetTableName<T>(table); 
            var sql = $"SELECT reltuples::bigint FROM pg_class WHERE relname = '{tableName}'";

            switch (Mode)
            {
                case PreferSqlMode.EFCore:
                    {
                        var result = await Database.SqlQueryRaw<long>(sql).FirstOrDefaultAsync();
                        return result;
                    }
                default:
                    {
                        var result = await ExecuteRawSqlOnceDirectly(sql);
                        return Convert.ToInt64(result);
                    }
            }
        }


        #endregion

        #region Exist

        /// <summary>
        /// 判断指定表中是否存在行。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>如果存在行则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async FTask<bool> Exist<T>(string table = null) where T : Entity
        {
            return await Set<T>().AnyAsync();
        }

        /// <summary>
        /// 判断指定表中是否存在满足条件的行。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="filter">用于筛选行的条件。</param>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>如果存在满足条件的行则返回 true，否则返回 false。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async FTask<bool> Exist<T>(Expression<Func<T, bool>> filter, string table = null) where T : Entity
        {
            return await Set<T>().AnyAsync(filter);
        }

        #endregion

        #region Query

        /// <summary>
        /// 在不加数据库锁定的情况下，查询指定 ID 的行。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="id">要查询的行 ID。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>查询到的行。</returns>
        public async FTask<T> QueryNotLock<T>(long id, bool isDeserialize = true, string table = null) where T : Entity
        {
            
            await FTask.CompletedTask;
            return null;
        }

        /// <summary>
        /// 查询指定 ID 的行，并加数据库锁定以确保数据一致性。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="id">要查询的行 ID。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>查询到的行。</returns>
        public async FTask<T> Query<T>(long id, bool isDeserialize = true, string table = null) where T : Entity
        {
            var entity = await Set<T>().FindAsync(id);
            if (!isDeserialize || entity==null)
            {
                return entity;
            }

            entity.Deserialize(pg.Scene, DeserializationRestore.FixEmbbeded);

            return entity;
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行数量和日期列表（不加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行数量和日期列表。</returns>
        public async FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, bool isDeserialize = true, string table = null) where T : Entity
        {
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     var count = await Count(kind);
            //     var dates = await QueryByPage(kind, pageIndex, pageSize, isDeserialize, table);
            //     return ((int)count, dates);
            // }
            await FTask.CompletedTask;
            return (0, new List<T>());
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行数量和日期列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行数量和日期列表。</returns>
        public async FTask<(int count, List<T> dates)> QueryCountAndDatesByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, bool isDeserialize = true, string table = null) where T : Entity
        {
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     var count = await Count(kind);
            //     var dates = await QueryByPage(kind, pageIndex, pageSize, cols, isDeserialize, table);
            //     return ((int)count, dates);
            // }
            await FTask.CompletedTask;
            return (0, new List<T>());
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行列表（不加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(kind);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {whereClause} LIMIT {pageSize} OFFSET {(pageIndex - 1) * pageSize}";
            //         var list1 = new Archetypes<P>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 list1.Add(entities);
            //             }
            //         }
            //         
            //         if (isDeserialize && list1.Count > 0)
            //         {
            //             foreach (var entities in list1)
            //             {
            //                 entities.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list1;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryByPage<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, string[] cols, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(kind);
            // var columns = cols != null && cols.Length > 0 ? string.Join(", ", cols.Select(c => $"\"{c}\"")) : "*";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT {columns} FROM \"{tableName}\" WHERE {whereClause} LIMIT {pageSize} OFFSET {(pageIndex - 1) * pageSize}";
            //         var list1 = new Archetypes<P>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 list1.Add(entities);
            //             }
            //         }
            //         
            //         if (isDeserialize && list1.Count > 0)
            //         {
            //             foreach (var entities in list1)
            //             {
            //                 entities.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list1;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 通过分页查询并返回满足条件的行列表，并按指定表达式进行排序（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="pageIndex">页码。</param>
        /// <param name="pageSize">每页大小。</param>
        /// <param name="orderByExpression">排序表达式。</param>
        /// <param name="isAsc">是否升序排序。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryByPageOrderBy<T>(Expression<Func<T, bool>> filter, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression, bool isAsc = true, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(kind);
            // var orderByColumn = GetColumnName(orderByExpression);
            // var orderDirection = isAsc ? "ASC" : "DESC";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {whereClause} ORDER BY \"{orderByColumn}\" {orderDirection} LIMIT {pageSize} OFFSET {(pageIndex - 1) * pageSize}";
            //         var list1 = new Archetypes<P>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 list1.Add(entities);
            //             }
            //         }
            //         
            //         if (isDeserialize && list1.Count > 0)
            //         {
            //             foreach (var entities in list1)
            //             {
            //                 entities.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list1;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的第一个行（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的第一个行，如果未找到则为 null。</returns>
        public async FTask<T?> First<T>(Expression<Func<T, bool>> filter, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(kind);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {whereClause} LIMIT 1";
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             if (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 
            //                 if (isDeserialize && entities != null)
            //                 {
            //                     entities.Deserialize(_scene);
            //                 }
            //                 
            //                 await _connection.CloseAsync();
            //                 return entities;
            //             }
            //         }
            //     }
            //     await _connection.CloseAsync();
            //     return null;
            // }
            await FTask.CompletedTask;
            return null;
        }

        /// <summary>
        /// 通过指定 JSON 格式查询并返回满足条件的第一个行（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的第一个行。</returns>
        public async FTask<T> First<T>(string json, string[] cols, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // var columns = cols != null && cols.Length > 0 ? string.Join(", ", cols.Select(c => $"\"{c}\"")) : "*";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         // 这里需要将JSON条件转换为SQL WHERE子句
            //         cmd.CommandText = $"SELECT {columns} FROM \"{tableName}\" WHERE {ConvertJsonToWhereClause(json)} LIMIT 1";
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             if (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 
            //                 if (isDeserialize && entities != null)
            //                 {
            //                     entities.Deserialize(_scene);
            //                 }
            //                 
            //                 await _connection.CloseAsync();
            //                 return entities;
            //             }
            //         }
            //     }
            //     await _connection.CloseAsync();
            //     return null;
            // }
            await FTask.CompletedTask;
            return null;
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的一行。
        /// 超过一行会报错(说明逻辑错误或数据库中存储了不在预期内的额外数据)。
        /// </summary>
        /// <returns>如果未找到则为 null。</returns>
        public async FTask<T?> SingleOrDefault<T>(Expression<Func<T, bool>> filter, bool isDeserialize = true, string table = null) where T : Entity
        {
            T res = null;
            using (await pg.FlowLock.WaitIfTooMuch())
            {
                res = await Set<T>().AsNoTracking().SingleOrDefaultAsync(filter);

                if (res == null)
                {
                    return res;
                }
            }

            if (isDeserialize)
            { 
                res.Deserialize(pg.Scene, DeserializationRestore.FixEmbbeded); 
            }
            
            return res;
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的行列表，并按指定表达式进行排序（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="orderByExpression">排序表达式。</param>
        /// <param name="isAsc">是否升序排序。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryOrderBy<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderByExpression, bool isAsc = true, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(kind);
            // var orderByColumn = GetColumnName(orderByExpression);
            // var orderDirection = isAsc ? "ASC" : "DESC";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {whereClause} ORDER BY \"{orderByColumn}\" {orderDirection}";
            //         var list1 = new Archetypes<P>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 list1.Add(entities);
            //             }
            //         }
            //         
            //         if (isDeserialize && list1.Count > 0)
            //         {
            //             foreach (var entities in list1)
            //             {
            //                 entities.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list1;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 通过指定过滤条件查询并返回满足条件的行列表。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, bool isDeserialize = true, string table = null) where T : Entity
        {
            List<T> list;
            using (await pg.FlowLock.WaitIfTooMuch())
            {
                list = await Set<T>().AsNoTracking().Where(filter).ToListAsync();
            }

            if (list == null || list.Count == 0 )          
                return list;

            if (isDeserialize)
            {
                foreach (var entity in list)
                {
                    entity.Deserialize(pg.Scene, DeserializationRestore.FixEmbbeded);
                }
            }
            return list;
        }

        /// <summary>
        /// 将查询结果装配到父级对象
        /// </summary>
        private IEnumerable<T> AppendFromDb<T>(IEnumerable<T> entities, Entity parent) where T : Entity 
        {
            if (entities == null || !entities.Any()) return entities;

            foreach (var entity in entities)
            {
                entity.Deserialize(pg.Scene, DeserializationRestore.FixEmbbeded);
                parent.AddComponent(entity);
            }
            return entities;
        }

        /// <summary>
        /// 从类型中获取整表名 (如果有命名空间, 带命名空间)
        /// <param name="isQuoted">是否带双引号, 默认为true ,适配SQL语句的嵌入规则。</param>
        /// </summary>
        private string GetFullTableName<T>(bool isQuoted = true)
        {
            IEntityType? entityType = Model.FindEntityType(typeof(T));

            if (entityType == null)
                throw new Exception($" This Type \"{typeof(T)}\" is not in built Entity-Table Model !");

            var tableName = entityType.GetTableName();
            var schema = entityType.GetSchema();
            string? res = default;
            if (isQuoted)
                res = string.IsNullOrEmpty(schema) ? $"\"{tableName}\"" : $"\"{schema}\".\"{tableName}\"";
            else
                res = string.IsNullOrEmpty(schema) ? tableName : $"{schema}{tableName}";

            if (res == null)
                throw new Exception($" Unexpected : \"{typeof(T)}\" has a NULL-TableName in built Entity-Table Model !");

            return res;
        }

        /// <summary>
        /// 查询子级且挂载: 查询某个父级上所有的某类实体, 并挂载到父级实体上。
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.EFCore"/>时 支持Linq表达式过滤。
        /// </para>
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.Dapper"/>时 不支持Linq表达式过滤。
        /// </para>
        /// </summary>
        /// <param name="parent">父级实体。</param>
        /// <param name="filter">过滤条件。</param>
        /// <param name="transaction">事务。</param>
        public async FTask<IEnumerable<T>> QueryAppend<T>(Entity parent, Expression<Func<T, bool>> filter = null,object transaction = null) where T : Entity
        {
            long parentType = TypeHashCache.GetHashCode(parent.Type);
            long parentId = parent.Id;

            if (Mode == PreferSqlMode.EFCore || filter != null)
            {
                IQueryable<T> query = Set<T>().AsNoTracking()
                        .Where(e =>
                            EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                            EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                        );

                if (filter != null)
                {
                    query = query.Where(filter);
                    if (Mode == PreferSqlMode.Dapper)
                        Log.Warning($"Dapper does not support Linq filter, this Query for \"{typeof(T)}\" appended on {parentId} has switched to EFCore-Mode automatically");
                }

                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    return AppendFromDb(await query.ToListAsync(), parent);
                }
            }
            else if (Mode == PreferSqlMode.Dapper)
            {
                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    var Connection = await GetOpenedConnection();

                    //统一事务, 直接传入EFCore的上下文事务需转为Dapper可用的数据库事务
                    var transa = transaction;
                    if (transaction is IDbContextTransaction contextTransa)
                        transa = contextTransa.GetDbTransaction();

                    IEnumerable<T> result = await Connection.QueryAsync<T>(
                    sql: $@"
                            {SQL.QUERY_BY_PARENT(GetFullTableName<T>())}
                            ",
                    transaction: transa as IDbTransaction,
                    param: new { ParentType = parentType, ParentId = parentId });
                    return AppendFromDb(result, parent);
                }
            }
            else throw new Exception("Unexpected : Unknown ORM Mode in PgSession.");
        }

        /// <summary>
        /// 查询子级且挂载: 查询某个父级上所有的某类实体, 并挂载到父级实体上。
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.EFCore"/>时 支持Linq表达式过滤。
        /// </para>
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.Dapper"/>时 不支持Linq表达式过滤。
        /// </para>
        /// </summary>
        /// <param name="parent">父级实体。</param>
        /// <param name="filter1">过滤条件1。</param>
        /// <param name="filter2">过滤条件2。</param>
        /// <param name="transaction">事务。</param>
        public async FTask<(IEnumerable<T1>, IEnumerable<T2>)> QueryAppend<T1, T2>(
                Entity parent,
                Expression<Func<T1, bool>> filter1 = null,
                Expression<Func<T2, bool>> filter2 = null,
                object transaction = null)
                where T1 : Entity
                where T2 : Entity
        {
            long parentType = TypeHashCache.GetHashCode(parent.Type);
            long parentId = parent.Id;

            if (Mode == PreferSqlMode.EFCore || filter1 != null || filter2 != null)
            {
                // EFCore 模式 , 构建两个独立的查询
                IQueryable<T1> query1 = Set<T1>().AsNoTracking()
                 .Where(e =>
                     EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                     EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                 );

                IQueryable<T2> query2 = Set<T2>().AsNoTracking()
                    .Where(e =>
                        EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                        EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                    );

                // 应用过滤器
                if (filter1 != null) query1 = query1.Where(filter1);
                if (filter2 != null) query2 = query2.Where(filter2);

                if (Mode == PreferSqlMode.Dapper && (filter1 != null || filter2 != null))
                {
                    // 记录 Dapper 模式下的自动切换
                    Log.Warning($"Dapper does not support Linq filter, this multi-query for \"{typeof(T1)}\"/\"{typeof(T2)}\" on {parent.Id} has switched to EFCore-Mode automatically");
                }

                var task1 = query1.ToListAsync();
                var task2 = query2.ToListAsync();

                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    await Task.WhenAll(task1, task2);

                    var t1Results = AppendFromDb(await task1, parent);
                    var t2Results = AppendFromDb(await task2, parent);

                    return (t1Results, t2Results);
                }
            }
            else if (Mode == PreferSqlMode.Dapper) // Dapper 模式(此模式下 filter1 和 filter2 必定为 null)
            {
                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    var Connection = await GetOpenedConnection();

                    string T1Name = GetFullTableName<T1>();
                    string T2Name = GetFullTableName<T2>();

                    //统一事务, 直接传入EFCore的上下文事务需转为Dapper可用的数据库事务
                    var transa = transaction;
                    if (transaction is IDbContextTransaction contextTransa)
                        transa = contextTransa.GetDbTransaction();

                    var multi = await Connection.QueryMultipleAsync(
                        sql: $@"
                            {SQL.QUERY_BY_PARENT(T1Name)}
                            {SQL.QUERY_BY_PARENT(T2Name)}
                            ",
                        transaction: transa as IDbTransaction,
                        param: new { ParentType = parentType, ParentId = parentId });

                    var t1Results = AppendFromDb(await multi.ReadAsync<T1>(), parent);
                    var t2Results = AppendFromDb(await multi.ReadAsync<T2>(), parent);

                    return (t1Results, t2Results);
                }
            }
            else
            {
                throw new Exception("Unexpected : Unknown ORM Mode in PgSession.");
            }
        }

        /// <summary>
        /// 查询子级且挂载: 查询某个父级上所有的某类实体, 并挂载到父级实体上。
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.EFCore"/>时 支持Linq表达式过滤。
        /// </para>
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.Dapper"/>时 不支持Linq表达式过滤。
        /// </para>
        /// </summary>
        /// <param name="parent">父级实体。</param>
        /// <param name="filter1">过滤条件1。</param>
        /// <param name="filter2">过滤条件2。</param>
        /// <param name="filter3">过滤条件3。</param>
        /// <param name="transaction">事务。</param>
        public async FTask<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>)> QueryAppend<T1, T2, T3>(
                Entity parent,
                Expression<Func<T1, bool>> filter1 = null,
                Expression<Func<T2, bool>> filter2 = null,
                Expression<Func<T3, bool>> filter3 = null,
                object transaction = null)
                where T1 : Entity
                where T2 : Entity
                where T3 : Entity
        {
            long parentType = TypeHashCache.GetHashCode(parent.Type);
            long parentId = parent.Id;

            if (Mode == PreferSqlMode.EFCore || filter1 != null || filter2 != null || filter3 != null)
            {
                // EFCore 模式 , 构建三个独立的查询
                IQueryable<T1> query1 = Set<T1>().AsNoTracking()
                 .Where(e =>
                     EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                     EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                 );

                IQueryable<T2> query2 = Set<T2>().AsNoTracking()
                     .Where(e =>
                         EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                         EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                     );

                IQueryable<T3> query3 = Set<T3>().AsNoTracking()
                     .Where(e =>
                         EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                         EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                     );

                // 应用过滤器
                if (filter1 != null) query1 = query1.Where(filter1);
                if (filter2 != null) query2 = query2.Where(filter2);
                if (filter3 != null) query3 = query3.Where(filter3);

                if (Mode == PreferSqlMode.Dapper && (filter1 != null || filter2 != null || filter3 != null))
                {
                    // 记录 Dapper 模式下的自动切换
                    Log.Warning($"Dapper does not support Linq filter, this multi-query for \"{typeof(T1)}\"/\"{typeof(T2)}\"/\"{typeof(T3)}\" on {parent.Id} has switched to EFCore-Mode automatically");
                }

                var task1 = query1.ToListAsync();
                var task2 = query2.ToListAsync();
                var task3 = query3.ToListAsync();

                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    await Task.WhenAll(task1, task2, task3);

                    var t1Results = AppendFromDb(await task1, parent);
                    var t2Results = AppendFromDb(await task2, parent);
                    var t3Results = AppendFromDb(await task3, parent);

                    return (t1Results, t2Results, t3Results);
                }
            }
            else if (Mode == PreferSqlMode.Dapper) // Dapper 模式(此模式下 filter 必定为 null)
            {
                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    var Connection = await GetOpenedConnection();
                    string T1Name = GetFullTableName<T1>();
                    string T2Name = GetFullTableName<T2>();
                    string T3Name = GetFullTableName<T3>();


                    //统一事务, 直接传入EFCore的上下文事务需转为Dapper可用的数据库事务
                    var transa = transaction;
                    if (transaction is IDbContextTransaction contextTransa)
                        transa = contextTransa.GetDbTransaction();

                    var multi = await Connection.QueryMultipleAsync(
                            sql: $@"
                        {SQL.QUERY_BY_PARENT(T1Name)}
                        {SQL.QUERY_BY_PARENT(T2Name)}
                        {SQL.QUERY_BY_PARENT(T3Name)}
                        ",
                        transaction: transa as IDbTransaction,
                        param: new { ParentType = parentType, ParentId = parentId });

                    var t1Results = AppendFromDb(await multi.ReadAsync<T1>(), parent);
                    var t2Results = AppendFromDb(await multi.ReadAsync<T2>(), parent);
                    var t3Results = AppendFromDb(await multi.ReadAsync<T3>(), parent);

                    return (t1Results, t2Results, t3Results);
                }
            }
            else
            {
                throw new Exception("Unexpected : Unknown ORM Mode in PgSession.");
            }
        }

        /// <summary>
        /// 查询子级且挂载: 查询某个父级上所有的某类实体, 并挂载到父级实体上。
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.EFCore"/>时 支持Linq表达式过滤。
        /// </para>
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.Dapper"/>时 不支持Linq表达式过滤。
        /// </para>
        /// </summary>
        /// <param name="parent">父级实体。</param>
        /// <param name="filter1">过滤条件1。</param>
        /// <param name="filter2">过滤条件2。</param>
        /// <param name="filter3">过滤条件3。</param>
        /// <param name="filter4">过滤条件4。</param>
        /// <param name="transaction">事务。</param>
        public async FTask<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>)> QueryAppend<T1, T2, T3, T4>(
                Entity parent,
                Expression<Func<T1, bool>> filter1 = null,
                Expression<Func<T2, bool>> filter2 = null,
                Expression<Func<T3, bool>> filter3 = null,
                Expression<Func<T4, bool>> filter4 = null,
                object? transaction = null)
                where T1 : Entity
                where T2 : Entity
                where T3 : Entity
                where T4 : Entity
        {
            long parentType = TypeHashCache.GetHashCode(parent.Type);
            long parentId = parent.Id;

            if (Mode == PreferSqlMode.EFCore || filter1 != null || filter2 != null || filter3 != null || filter4 != null)
            {
                // EFCore 模式 , 构建四个独立的查询
                IQueryable<T1> query1 = Set<T1>().AsNoTracking()
                 .Where(e =>
                     EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                     EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                 );

                IQueryable<T2> query2 = Set<T2>().AsNoTracking()
                     .Where(e =>
                         EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                         EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                     );

                IQueryable<T3> query3 = Set<T3>().AsNoTracking()
                     .Where(e =>
                         EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                         EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                     );

                IQueryable<T4> query4 = Set<T4>().AsNoTracking()
                     .Where(e =>
                         EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                         EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                     );

                // 应用过滤器
                if (filter1 != null) query1 = query1.Where(filter1);
                if (filter2 != null) query2 = query2.Where(filter2);
                if (filter3 != null) query3 = query3.Where(filter3);
                if (filter4 != null) query4 = query4.Where(filter4);

                if (Mode == PreferSqlMode.Dapper && (filter1 != null || filter2 != null || filter3 != null || filter4 != null))
                {
                    // 记录 Dapper 模式下的自动切换
                    Log.Warning($"Dapper does not support Linq filter, this multi-query for \"{typeof(T1)}\"/\"{typeof(T2)}\"/\"{typeof(T3)}\"/\"{typeof(T4)}\" on {parent.Id} has switched to EFCore-Mode automatically");
                }

                var task1 = query1.ToListAsync();
                var task2 = query2.ToListAsync();
                var task3 = query3.ToListAsync();
                var task4 = query4.ToListAsync();

                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    await Task.WhenAll(task1, task2, task3, task4);

                    var t1Results = AppendFromDb(await task1, parent);
                    var t2Results = AppendFromDb(await task2, parent);
                    var t3Results = AppendFromDb(await task3, parent);
                    var t4Results = AppendFromDb(await task4, parent);

                    return (t1Results, t2Results, t3Results, t4Results);
                }
            }
            else if (Mode == PreferSqlMode.Dapper) // Dapper 模式(此模式下 filter 必定为 null)
            {
                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    var Connection = await GetOpenedConnection();
                    string T1Name = GetFullTableName<T1>();
                    string T2Name = GetFullTableName<T2>();
                    string T3Name = GetFullTableName<T3>();
                    string T4Name = GetFullTableName<T4>();

                    var transa = transaction;
                    if (transaction is IDbContextTransaction contextTransa)
                        transa = contextTransa.GetDbTransaction();

                    var multi = await Connection.QueryMultipleAsync(
                        sql: $@"
                        {SQL.QUERY_BY_PARENT(T1Name)}
                        {SQL.QUERY_BY_PARENT(T2Name)}
                        {SQL.QUERY_BY_PARENT(T3Name)}
                        {SQL.QUERY_BY_PARENT(T4Name)}
                        ",
                        transaction: transa as IDbTransaction,
                        param: new { ParentType = parentType, ParentId = parentId });

                    var t1Results = AppendFromDb(await multi.ReadAsync<T1>(), parent);
                    var t2Results = AppendFromDb(await multi.ReadAsync<T2>(), parent);
                    var t3Results = AppendFromDb(await multi.ReadAsync<T3>(), parent);
                    var t4Results = AppendFromDb(await multi.ReadAsync<T4>(), parent);

                    return (t1Results, t2Results, t3Results, t4Results);
                }
            }
            else
            {
                throw new Exception("Unexpected : Unknown ORM Mode in PgSession.");
            }
        }

        /// <summary>
        /// 查询子级且挂载: 查询某个父级上所有的某类实体, 并挂载到父级实体上。
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.EFCore"/>时 支持Linq表达式过滤。
        /// </para>
        /// <para>
        /// <see cref="Mode"/>为<see cref="PreferSqlMode.Dapper"/>时 不支持Linq表达式过滤。
        /// </para>
        /// </summary>
        /// <param name="parent">父级实体。</param>
        /// <param name="filter1">过滤条件1。</param>
        /// <param name="filter2">过滤条件2。</param>
        /// <param name="filter3">过滤条件3。</param>
        /// <param name="filter4">过滤条件4。</param>
        /// <param name="filter5">过滤条件5。</param>
        /// <param name="transaction">事务。</param>
        public async FTask<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>)> QueryAppend<T1, T2, T3, T4, T5>(
                Entity parent,
                Expression<Func<T1, bool>> filter1 = null,
                Expression<Func<T2, bool>> filter2 = null,
                Expression<Func<T3, bool>> filter3 = null,
                Expression<Func<T4, bool>> filter4 = null,
                Expression<Func<T5, bool>> filter5 = null,
                object? transaction = null)
                where T1 : Entity
                where T2 : Entity
                where T3 : Entity
                where T4 : Entity
                where T5 : Entity
        {
            long parentType = TypeHashCache.GetHashCode(parent.Type);
            long parentId = parent.Id;

            if (Mode == PreferSqlMode.EFCore || filter1 != null || filter2 != null || filter3 != null || filter4 != null || filter5 != null)
            {
                // EFCore 模式 , 构建五个独立的查询
                IQueryable<T1> query1 = Set<T1>().AsNoTracking()
                 .Where(e =>
                     EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                     EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                 );

                IQueryable<T2> query2 = Set<T2>().AsNoTracking()
                     .Where(e =>
                         EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                         EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                     );

                IQueryable<T3> query3 = Set<T3>().AsNoTracking()
                     .Where(e =>
                         EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                         EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                     );

                IQueryable<T4> query4 = Set<T4>().AsNoTracking()
                     .Where(e =>
                         EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                         EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                     );

                IQueryable<T5> query5 = Set<T5>().AsNoTracking()
                     .Where(e =>
                         EF.Property<long>(e, DbSetProperty.ParentType) == parentType &&
                         EF.Property<long>(e, DbSetProperty.ParentId) == parentId
                     );

                // 应用过滤器
                if (filter1 != null) query1 = query1.Where(filter1);
                if (filter2 != null) query2 = query2.Where(filter2);
                if (filter3 != null) query3 = query3.Where(filter3);
                if (filter4 != null) query4 = query4.Where(filter4);
                if (filter5 != null) query5 = query5.Where(filter5);

                if (Mode == PreferSqlMode.Dapper && (filter1 != null || filter2 != null || filter3 != null || filter4 != null || filter5 != null))
                {
                    // 记录 Dapper 模式下的自动切换
                    Log.Warning($"Dapper does not support Linq filter, this multi-query for \"{typeof(T1)}\"/\"{typeof(T2)}\"/\"{typeof(T3)}\"/\"{typeof(T4)}\"/\"{typeof(T5)}\" on {parent.Id} has switched to EFCore-Mode automatically");
                }

                var task1 = query1.ToListAsync();
                var task2 = query2.ToListAsync();
                var task3 = query3.ToListAsync();
                var task4 = query4.ToListAsync();
                var task5 = query5.ToListAsync();

                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    await Task.WhenAll(task1, task2, task3, task4, task5);

                    var t1Results = AppendFromDb(await task1, parent);
                    var t2Results = AppendFromDb(await task2, parent);
                    var t3Results = AppendFromDb(await task3, parent);
                    var t4Results = AppendFromDb(await task4, parent);
                    var t5Results = AppendFromDb(await task5, parent);

                    return (t1Results, t2Results, t3Results, t4Results, t5Results);
                }
            }
            else if (Mode == PreferSqlMode.Dapper) // Dapper 模式(此模式下 filter 必定为 null)
            {
                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    var Connection = await GetOpenedConnection();
                    string T1Name = GetFullTableName<T1>();
                    string T2Name = GetFullTableName<T2>();
                    string T3Name = GetFullTableName<T3>();
                    string T4Name = GetFullTableName<T4>();
                    string T5Name = GetFullTableName<T5>();

                    var transa = transaction;
                    if (transaction is IDbContextTransaction contextTransa)
                        transa = contextTransa.GetDbTransaction();

                    var multi = await Connection.QueryMultipleAsync(
                        sql: $@"
                    {SQL.QUERY_BY_PARENT(T1Name)}
                    {SQL.QUERY_BY_PARENT(T2Name)}
                    {SQL.QUERY_BY_PARENT(T3Name)}
                    {SQL.QUERY_BY_PARENT(T4Name)}
                    {SQL.QUERY_BY_PARENT(T5Name)}
                    ",
                         transaction: transa as IDbTransaction,
                        param: new { ParentType = parentType, ParentId = parentId });

                    var t1Results = AppendFromDb(await multi.ReadAsync<T1>(), parent);
                    var t2Results = AppendFromDb(await multi.ReadAsync<T2>(), parent);
                    var t3Results = AppendFromDb(await multi.ReadAsync<T3>(), parent);
                    var t4Results = AppendFromDb(await multi.ReadAsync<T4>(), parent);
                    var t5Results = AppendFromDb(await multi.ReadAsync<T5>(), parent);

                    return (t1Results, t2Results, t3Results, t4Results, t5Results);
                }
            }
            else
            {
                throw new Exception("Unexpected : Unknown ORM Mode in PgSession.");
            }
        }

        /// <summary>
        /// TODO : JOIN, 目前不适用, 得想清楚JOIN适合什么地方再写一个新的。
        /// JOIN 可能更适合查某组实体分别连带出不同的引用的数据, 即N:1的场景
        /// </summary>
        public async FTask<IEnumerable<TResult>> QueryJoin<TResult>(Type[] types, Func<object[], TResult> mapFunc, Entity parent, bool isDeserialize = true, string table = null)
        {
            long parentType = TypeHashCache.GetHashCode(parent.Type);
            long parentId = parent.Id;

            var JOIN_SQL = @"

            ";

            using (await pg.FlowLock.WaitIfTooMuch())
            {
                var Connection = await GetOpenedConnection();

                IEnumerable<TResult> result = await Connection.QueryAsync(
                    sql: JOIN_SQL,
                    map: mapFunc,
                    param: new { ParentType = parentType, ParentId = parentId },
                    splitOn: DbSetProperty.MultiEntitiesRowSplitOn,
                    types: types
                    );

                if (!isDeserialize || result.Count() == 0)
                {
                    return result;
                }

                foreach (TResult? res in result)
                {
                    //TODO

                }
                return result;
            }
        }

        /// <summary>
        /// 根据指定 ID 加锁查询多个表中的行。
        /// </summary>
        /// <param name="id">行 ID。</param>
        /// <param name="tableNames">要查询的表名称列表。</param>
        /// <param name="result">查询结果存储列表。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        public async FTask Query(long id, List<string>? tableNames, List<Entity> result, bool isDeserialize = true)
        {
            // if (tableNames == null || tableNames.Count == 0)
            // {
            //     return;
            // }
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     foreach (var tableName in tableNames)
            //     {
            //         using (var cmd = _connection.CreateCommand())
            //         {
            //             cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //             cmd.Parameters.AddWithValue("Id", id);
            //             using (var reader = await cmd.ExecuteReaderAsync())
            //             {
            //                 if (await reader.ReadAsync())
            //                 {
            //                     var bsonDocument = GetBsonDocumentFromReader(reader);
            //                     var entityType = Type.GetType($"Fantasy.Entities.{tableName}, Fantasy");
            //                     if (entityType != null && typeof(ToParentIs).IsAssignableFrom(entityType))
            //                     {
            //                         var entities = _serializer.Deserialize(entityType, bsonDocument) as ToParentIs;
            //                         if (isDeserialize && entities != null)
            //                         {
            //                             entities.Deserialize(_scene);
            //                         }
            //                         entities.Add(entities);
            //                     }
            //                 }
            //             }
            //         }
            //     }
            //     await _connection.CloseAsync();
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 根据指定的 JSON 查询条件查询并返回满足条件的行列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryJson<T>(string json, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         // 这里需要将JSON条件转换为SQL WHERE子句
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {ConvertJsonToWhereClause(json)}";
            //         var list1 = new Archetypes<P>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 list1.Add(entities);
            //             }
            //         }
            //         
            //         if (isDeserialize && list1.Count > 0)
            //         {
            //             foreach (var entities in list1)
            //             {
            //                 entities.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list1;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 根据指定的 JSON 查询条件查询并返回满足条件的行列表，并选择指定的列（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryJson<T>(string json, string[] cols, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // var columns = cols != null && cols.Length > 0 ? string.Join(", ", cols.Select(c => $"\"{c}\"")) : "*";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         // 这里需要将JSON条件转换为SQL WHERE子句
            //         cmd.CommandText = $"SELECT {columns} FROM \"{tableName}\" WHERE {ConvertJsonToWhereClause(json)}";
            //         var list1 = new Archetypes<P>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 list1.Add(entities);
            //             }
            //         }
            //         
            //         if (isDeserialize && list1.Count > 0)
            //         {
            //             foreach (var entities in list1)
            //             {
            //                 entities.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list1;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 根据指定的 JSON 查询条件和任务 ID 查询并返回满足条件的行列表（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="taskId">任务 ID。</param>
        /// <param name="json">JSON 查询条件。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> QueryJson<T>(long taskId, string json, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // 
            // using (await _dataBaseLock.Wait(taskId))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         // 这里需要将JSON条件转换为SQL WHERE子句
            //         cmd.CommandText = $"SELECT * FROM \"{tableName}\" WHERE {ConvertJsonToWhereClause(json)}";
            //         var list1 = new Archetypes<P>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 list1.Add(entities);
            //             }
            //         }
            //         
            //         if (isDeserialize && list1.Count > 0)
            //         {
            //             foreach (var entities in list1)
            //             {
            //                 entities.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list1;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 根据指定过滤条件查询并返回满足条件的行列表，选择指定的列（加锁）。
        /// </summary>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <param name="filter">查询过滤条件。</param>
        /// <param name="cols">要查询的列名称数组。</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名称。</param>
        /// <returns>满足条件的行列表。</returns>
        public async FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, string[] cols, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(filter);
            // var columns = cols != null && cols.Length > 0 ? string.Join(", ", cols.Select(c => $"\"{c}\"")) : "*";
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT {columns} FROM \"{tableName}\" WHERE {whereClause}";
            //         var list1 = new Archetypes<P>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 list1.Add(entities);
            //             }
            //         }
            //         
            //         if (isDeserialize && list1.Count > 0)
            //         {
            //             foreach (var entities in list1)
            //             {
            //                 entities.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list1;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        /// <summary>
        /// 根据指定过滤条件查询并返回满足条件的行列表，选择指定的列（加锁）。
        /// </summary>
        /// <param name="filter">查询过滤条件</param>
        /// <param name="cols">要查询的列名称数组</param>
        /// <param name="isDeserialize">是否在查询后反序列化,执行反序列化后会自动将实体注册到框架系统中，并且能正常使用组件相关功能。</param>
        /// <param name="table">表名。</param>
        /// <typeparam name="T">文档实体类型。</typeparam>
        /// <returns></returns>
        public async FTask<List<T>> Query<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>>[] cols, bool isDeserialize = true, string table = null) where T : Entity
        {
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(filter);
            // var columns = new Archetypes<string> { "\"Id\"" }; // 确保包含Id列
            // 
            // foreach (var col in cols)
            // {
            //     if (col.Body is MemberExpression memberExpression)
            //     {
            //         columns.Add($"\"{memberExpression.Member.Name}\"");
            //     }
            //     else if (col.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression)
            //     {
            //         columns.Add($"\"{((MemberExpression)unaryExpression.Operand).Member.Name}\"");
            //     }
            // }
            // 
            // var columnsStr = string.Join(", ", columns);
            // 
            // using (await _dataBaseLock.Wait(RandomHelper.RandInt64() % DefaultTaskSize))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"SELECT {columnsStr} FROM \"{tableName}\" WHERE {whereClause}";
            //         var list1 = new Archetypes<P>();
            //         using (var reader = await cmd.ExecuteReaderAsync())
            //         {
            //             while (await reader.ReadAsync())
            //             {
            //                 var bsonDocument = GetBsonDocumentFromReader(reader);
            //                 var entities = _serializer.Deserialize<P>(bsonDocument);
            //                 list1.Add(entities);
            //             }
            //         }
            //         
            //         if (isDeserialize && list1.Count > 0)
            //         {
            //             foreach (var entities in list1)
            //             {
            //                 entities.Deserialize(_scene);
            //             }
            //         }
            //         
            //         await _connection.CloseAsync();
            //         return list1;
            //     }
            // }
            await FTask.CompletedTask;
            return new List<T>();
        }

        #endregion

        #region Save

        /// <summary>
        /// 保存实体对象到数据库（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="entity">要保存的实体对象。</param>
        /// <param name="table">表名称。</param>
        public async FTask Save<T>(object transactionSession, T? entity, string table = null) where T : Entity
        {
            // if (entities == null)
            // {
            //     Log.Error($"save entities is null: {typeof(P).Name}");
            //     return;
            // }
            // 
            // var clone = _serializer.Clone(entities);
            // var tableName = GetTableName<P>(table);
            // 
            // using (await _dataBaseLock.Wait(clone.Id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.Transaction = (NpgsqlTransaction)transaction;
            //         var columns = GetColumnsForEntity(clone);
            //         var values = GetValuesForEntity(clone);
            //         
            //         // 检查记录是否存在
            //         cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //         cmd.Parameters.Clear();
            //         cmd.Parameters.AddWithValue("Id", clone.Id);
            //         var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            //         
            //         if (count > 0)
            //         {
            //             // 更新
            //             var setClause = string.Join(", ", columns.Select(c => $"\"{c}\" = @{c}"));
            //             cmd.CommandText = $"UPDATE \"{tableName}\" SET {setClause} WHERE \"Id\" = @Id";
            //             AddParametersForEntity(cmd, clone, columns);
            //         }
            //         else
            //         {
            //             // 插入
            //             var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            //             var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
            //             cmd.CommandText = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valueList})";
            //             AddParametersForEntity(cmd, clone, columns);
            //         }
            //         
            //         await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //     }
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 保存实体对象到数据库（全量）。
        /// </summary>
        public async FTask Save<T>(T? entity, string table = null) where T : Entity, new()
        {
            if (entity == null)
            {
                Log.Error($"Entity is null: {entity.GetType()}");
                return;
            }

            var tableName = GetTableName<T>(table);

            using (await pg.FlowLock.Wait(entity.Id))
            {
                switch (Mode)
                {
                    case PreferSqlMode.EFCore:
                        {
                            Entry(entity).State = EntityState.Modified;
                            await SaveChangesAsync();
                            break;
                        }
                    case PreferSqlMode.Dapper:
                        {
                            var connection = await GetOpenedConnection();

                            // TODO
                            var sql = $@"
                                    ";

                            await connection.ExecuteAsync(sql, entity);

                            break;
                        }
                }              
            }
        }

        /// <summary>
        /// 保存实体对象到数据库（部分字段）。
        /// </summary>
        public async FTask SavePartial<T>(T? entity, string table = null, params string[] propertyNames) where T : Entity, new()
        {
            if (entity == null)
            {
                Log.Error($"Entity is null: {entity.GetType()}");
                return;
            }

            using (await pg.FlowLock.Wait(entity.Id))
            {
                switch (Mode)
                {
                    case PreferSqlMode.EFCore:
                        {
                            Attach(entity);
                            var entry = Entry(entity);

                            foreach (var field in propertyNames)
                            {
                                entry.Property(field).IsModified = true;
                            }

                            await SaveChangesAsync();
                            break;
                        }
                    case PreferSqlMode.Dapper:
                        {
                            var connection = await GetOpenedConnection();

                            // TODO
                            var sql = $@"
                                    ";

                            await connection.ExecuteAsync(sql, entity);

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// 保存实体对象到数据库（加锁）。
        /// </summary>
        /// <param name="filter">保存的条件表达式。</param>
        /// <param name="entity">实体类型。</param>
        /// <param name="table">表名称。</param>
        /// <typeparam name="T"></typeparam>
        public async FTask Save<T>(Expression<Func<T, bool>> filter, T? entity, string table = null) where T : Entity, new()
        {
            // if (entities == null)
            // {
            //     Log.Error($"save entities is null: {typeof(P).Name}");
            //     return;
            // }
            // 
            // var clone = _serializer.Clone(entities);
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(filter);
            // 
            // using (await _dataBaseLock.Wait(clone.Id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         var columns = GetColumnsForEntity(clone);
            //         var setClause = string.Join(", ", columns.Select(c => $"\"{c}\" = @{c}"));
            //         
            //         cmd.CommandText = $"UPDATE \"{tableName}\" SET {setClause} WHERE {whereClause}";
            //         AddParametersForEntity(cmd, clone, columns);
            //         
            //         await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //     }
            // }
            await FTask.CompletedTask;
        }

        /// <summary>
        /// 保存多个实体对象到数据库（加锁）。
        /// </summary>
        /// <param name="id">行 ID。</param>
        /// <param name="entities">要保存的实体对象列表。</param>
        public async FTask Save(long id, List<Entity>? entities)
        {
            // if (entities == null || entities.Count == 0)
            // {
            //     Log.Error("save entities is null");
            //     return;
            // }
            // 
            // using var listPool = ListPool<ToParentIs>.Create();
            // 
            // foreach (var entities in entities)
            // {
            //     listPool.Add(_serializer.Clone(entities)); 
            // }
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     foreach (var clone in listPool)
            //     {
            //         try
            //         {
            //             var tableName = clone.GetType().Name;
            //             var columns = GetColumnsForEntity(clone);
            //             
            //             // 检查记录是否存在
            //             using (var cmd = _connection.CreateCommand())
            //             {
            //                 cmd.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //                 cmd.Parameters.Clear();
            //                 cmd.Parameters.AddWithValue("Id", clone.Id);
            //                 var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            //                 
            //                 if (count > 0)
            //                 {
            //                     // 更新
            //                     var setClause = string.Join(", ", columns.Select(c => $"\"{c}\" = @{c}"));
            //                     cmd.CommandText = $"UPDATE \"{tableName}\" SET {setClause} WHERE \"Id\" = @Id";
            //                     AddParametersForEntity(cmd, clone, columns);
            //                 }
            //                 else
            //                 {
            //                     // 插入
            //                     var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            //                     var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
            //                     cmd.CommandText = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valueList})";
            //                     AddParametersForEntity(cmd, clone, columns);
            //                 }
            //                 
            //                 await cmd.ExecuteNonQueryAsync();
            //             }
            //         }
            //         catch (Exception e)
            //         {
            //             Log.Error($"Save Archetypes ToParentIs Error: {clone.GetType().Name} {clone}\n{e}");
            //         }
            //     }
            //     await _connection.CloseAsync();
            // }
            await FTask.CompletedTask;
        }

        #endregion

        #region Insert

        /// <summary>
        /// 插入单个实体对象到数据库。
        /// 插入操作仅支持使用EFCore, 不支持Dapper。 
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="entity">要插入的实体对象。</param>
        /// <param name="table">表名称。</param>
        /// <param name="transaction">事务,因为仅支持EFCore,由外部跟踪事务,不需要显式传入。</param>
        public async FTask Insert<T>(T? entity, string table = null,object? transaction = null) where T : Entity, new()
        {
            if (entity == null)
            {
                throw new($" Null entity can not Insert.");
            }

            if (!TypeDbSetChecker<T>.IsDbSet || TypeDbSetChecker<T>.IsEmbedded)
            {
                throw new($"Can not Insert {entity.Id} due to type \"{typeof(T)}\" is not with [DbSet] or is set as Embedded. ");
            }
            //if(entity is not IDbSet dbSet || entity.IsEmbeddedDbSet())
            //{
            //    throw new($"Can not Insert {entity.Id} due to type \"{typeof(T)}\" is not with [DbSet] or is set as Embedded. ");
            //}

            try
            {
#if DESIGN_TIME
                var single = entity.Single;
                int singleCount = single == null ? 0:single.Count();
                int embbededCount = 0;
                if(single!=null)
                {
                    foreach (var kv in single)
                    {
                        if (kv.Value.IsAnnotatedAsEmbedded() == true)
                            embbededCount++;
                    }
                }
                Log.Debug($"{entity.Type.Name}中有{singleCount}个single(s),其中{embbededCount}个嵌入");
                Log.Debug($"{entity.Type.Name}转为Json: \n{entity.ToJson(new JsonSettings(Library.Microsoft),true)}");
#endif
                using (await pg.FlowLock.Wait(entity.Id))
                {
                    if (TypeDbSetChecker<T>.IsAsDoc)
                    {
                        //----------文档式存储----------
                        EntityDocumentDTC docData = MultiThreadPoolStacks.Rent<EntityDocumentDTC>();
                        docData.ParentId = entity.Parent.Id;
                        docData.ParentType = entity.Parent.TypeHashCode;
                        docData.Json = entity;

                        var shadowDbSet = Set<EntityDocumentDTC>(TypeDbSetChecker<T>.ShadowName!);
                        shadowDbSet.Add(docData);

                        var count = await SaveChangesAsync();

                        MultiThreadPoolStacks.Return(docData);
                        Entry(docData).State = EntityState.Detached;
                    }
                    else
                    {
                        //----------表格式存储----------
                        var entry = Entry(entity);
                        Set<T>().Add(entity);
                        var parent = entity.Parent;
                        if (parent != null)
                        {
                            //更新影子属性的值
                            Entry(entity).Property<long>(DbSetProperty.ParentType).CurrentValue = parent.TypeHashCode;
                            Entry(entity).Property<long>(DbSetProperty.ParentId).CurrentValue = parent.Id;
                        }
                        //更新影子属性的值
                        //Entry(entity).Property<ReuseList<Entity>>(DbSetProperty.JsonSingle).CurrentValue = entity.EmbbededSingle;
                        //Entry(entity).Property<ReuseList<Entity>>(DbSetProperty.JsonMulti).CurrentValue = entity.EmbbededMulti;

                        //Note: 暂时不支持二进制存储
                        //Entry(entity).Property<byte[]>(DbSetProperty.BytesSingle).CurrentValue = 1;
                        //Entry(entity).Property<byte[]>(DbSetProperty.BytesMulti).CurrentValue = 1;
                        var count = await SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex) { 
                throw new($"{pg.GetDatabaseType} Insert-Err ({entity.Type}:{entity.Id}) !\n {ex} ");
            }
        }

        /// <summary>
        /// 批量插入实体对象列表到数据库。
        /// 插入操作仅支持使用EFCore, 不支持Dapper。 
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="list">要插入的实体对象列表。</param>
        /// <param name="table">可选表名称。</param>
        /// <param name="transaction">事务,因为仅支持EFCore,由外部跟踪事务,不需要显式传入。</param>
        public async FTask InsertBatch<T>(IEnumerable<T> list, string table = null, object? transaction=null) where T : Entity, new()
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list), "InsertBatch: list1 can not be null.");

            var validList = ReuseList<T>.Create();

            foreach (var entity in list)
            {
                if (entity == null)
                {
                    Log.Error($"InsertBatch: Skipped null entity.");
                    continue;
                }

                if (!TypeDbSetChecker<T>.IsDbSet || TypeDbSetChecker<T>.IsEmbedded)
                {
                    Log.Warning($"InsertBatch: Skipped entity {entity.Id} due to type \"{typeof(T)}\" not a DbSet or marked as Embedded.");
                    continue;
                }

                // 更新影子索引
                var parent = entity.Parent;
                if (parent != null)
                {
                    Entry(entity).Property(DbSetProperty.ParentType).CurrentValue = parent.TypeHashCode;
                    Entry(entity).Property(DbSetProperty.ParentId).CurrentValue = parent.Id;
                }

                validList.Add(entity);
            }

            if (!validList.Any())
            {
                Log.Error("InsertBatch: No valid entities to insert.");
                return;
            }

            try
            {
                using (await pg.FlowLock.WaitIfTooMuch())
                {
                    await Set<T>().AddRangeAsync(validList);
                    var count = await SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                var ids = string.Join(", ", validList.Select(e => e.Id));
                throw new Exception($"{pg.GetDatabaseType} InsertBatch-Err ({typeof(T)} ids: {ids})!\n{ex}");
            }
            finally
            {
                validList.Dispose();
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
            // var tableName = GetTableName<P>();
            // 
            // using (await _dataBaseLock.Wait(taskId))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         var columns = bsonDocument.Names;
            //         var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
            //         var valueList = string.Join(", ", columns.Select(c => $"@{c}"));
            //         
            //         cmd.CommandText = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({valueList})";
            //         foreach (var name in columns)
            //         {
            //             cmd.Parameters.AddWithValue(name, bsonDocument[name].RawValue);
            //         }
            //         
            //         await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //     }
            // }
            await FTask.CompletedTask;
        }

        #endregion

        #region Remove

        /// <summary>
        /// 根据ID删除单个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="id">要删除的实体的ID。</param>
        /// <param name="table">表名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(object transactionSession, long id, string table = null)
            where T : Entity, new()
        {
            // var tableName = GetTableName<P>(table);
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.Transaction = (NpgsqlTransaction)transaction;
            //         cmd.CommandText = $"DELETE FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //         cmd.Parameters.AddWithValue("Id", id);
            //         var entities = await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //         return entities;
            //     }
            // }
            await FTask.CompletedTask;
            return 0;
        }

        /// <summary>
        /// 根据ID删除单个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="id">要删除的实体的ID。</param>
        /// <param name="table">表名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(long id, string table = null) where T : Entity, new()
        {
            // var tableName = GetTableName<P>(table);
            // 
            // using (await _dataBaseLock.Wait(id))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"DELETE FROM \"{tableName}\" WHERE \"Id\" = @Id";
            //         cmd.Parameters.AddWithValue("Id", id);
            //         var entities = await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //         return entities;
            //     }
            // }
            await FTask.CompletedTask;
            return 0;
        }

        /// <summary>
        /// 根据ID和筛选条件删除多个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="coroutineLockQueueKey">异步锁Id。</param>
        /// <param name="transactionSession">事务会话对象。</param>
        /// <param name="filter">筛选条件。</param>
        /// <param name="table">表名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(long coroutineLockQueueKey, object transactionSession,
            Expression<Func<T, bool>> filter, string table = null) where T : Entity, new()
        {
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(kind);
            // 
            // using (await _dataBaseLock.Wait(coroutineLockQueueKey))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.Transaction = (NpgsqlTransaction)transaction;
            //         cmd.CommandText = $"DELETE FROM \"{tableName}\" WHERE {whereClause}";
            //         var entities = await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //         return entities;
            //     }
            // }
            await FTask.CompletedTask;
            return 0;
        }

        /// <summary>
        /// 根据ID和筛选条件删除多个实体对象（加锁）。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="coroutineLockQueueKey">异步锁Id。</param>
        /// <param name="filter">筛选条件。</param>
        /// <param name="table">表名称。</param>
        /// <returns>删除的实体数量。</returns>
        public async FTask<long> Remove<T>(long coroutineLockQueueKey, Expression<Func<T, bool>> filter,
            string table = null) where T : Entity, new()
        {
            // var tableName = GetTableName<P>(table);
            // var whereClause = GetWhereClause(kind);
            // 
            // using (await _dataBaseLock.Wait(coroutineLockQueueKey))
            // {
            //     await _connection.OpenAsync();
            //     using (var cmd = _connection.CreateCommand())
            //     {
            //         cmd.CommandText = $"DELETE FROM \"{tableName}\" WHERE {whereClause}";
            //         var entities = await cmd.ExecuteNonQueryAsync();
            //         await _connection.CloseAsync();
            //         return entities;
            //     }
            // }
            await FTask.CompletedTask;
            return 0;
        }

        #endregion

        #region Utility

        /// <summary>
        /// ***************************** AI写的, 待人工检验 ********************************
        /// 对满足条件的行中的某个数值字段进行求和操作。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="filter">用于筛选行的条件。</param>
        /// <param name="sumExpression">要对其进行求和的字段表达式。</param>
        /// <param name="table">表名称，可选。如果未指定，将使用实体类型的名称。</param>
        /// <returns>满足条件的行中指定字段的求和结果。</returns>
        public async FTask<long> Sum<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> sumExpression, string? table = null) where T : Entity
        {
            //var tableName = table ?? typeof(P).Name.ToLowerInvariant();
            //var columnName = GetColumnName(sumExpression);

            //var (whereClause, parameters) = BuildSqlWhereClause(kind); //这里调用之后进行了复杂的构建

            //await using var conn = await Handler.OpenConnectionAsync();
            //await using var cmd = conn.CreateCommand();

            //// 使用参数化查询避免 SQL 注入
            //cmd.CommandText = $"SELECT SUM({columnName}) FROM \"{tableName}\" WHERE {whereClause}";
            //foreach (var param in parameters)
            //    cmd.Parameters.Add(param);

            //var entities = await cmd.ExecuteScalarAsync();
            //return entities != DBNull.Value ? Convert.ToInt64(entities) : 0;
            await FTask.CompletedTask;
            return 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearTracker() {
            ChangeTracker.Clear();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 从DataReader中获取BsonDocument
        /// </summary>
        private BsonDocument GetBsonDocumentFromReader(NpgsqlDataReader reader)
        {
            // var document = new BsonDocument();
            // for (int i = 0; i < reader.FieldCount; i++)
            // {
            //     var name = reader.GetName(i);
            //     var value = reader.GetValue(i);
            //     
            //     if (value == DBNull.Value)
            //     {
            //         document.Add(name, BsonNull.Value);
            //     }
            //     else
            //     {
            //         document.Add(name, new BsonValue(value));
            //     }
            // }
            // return document;
            return new BsonDocument();
        }

        /// <summary>
        /// 将JSON查询条件转换为SQL WHERE子句
        /// </summary>
        private string ConvertJsonToWhereClause(string json)
        {
            // 这里应该实现JSON到SQL WHERE子句的转换
            // 为简化，返回一个占位符
            return "1=1";
        }

        /// <summary>
        /// 获取实体的所有列名
        /// </summary>
        private List<string> GetColumnsForEntity<T>(T entity) where T : Entity
        {
            // var columns = new Archetypes<string>();
            // var parentProperties = typeof(P).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            // foreach (var property in parentProperties)
            // {
            //     columns.Add(property.Name);
            // }
            // return columns;
            return new List<string>();
        }

        /// <summary>
        /// 获取实体的所有值
        /// </summary>
        private List<object> GetValuesForEntity<T>(T entity) where T : Entity
        {
            // var values = new Archetypes<object>();
            // var parentProperties = typeof(P).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            // foreach (var property in parentProperties)
            // {
            //     values.Add(property.GetValue(entities));
            // }
            // return values;
            return new List<object>();
        }

        /// <summary>
        /// 为命令添加实体参数
        /// </summary>
        private void AddParametersForEntity<T>(NpgsqlCommand cmd, T entity, List<string> columns) where T : Entity
        {
            // foreach (var column in columns)
            // {
            //     var property = typeof(P).GetProperty(column);
            //     if (property != null)
            //     {
            //         var value = property.GetValue(entities);
            //         cmd.Parameters.AddWithValue(column, value ?? DBNull.Value);
            //     }
            // }
        }

        /// <summary>
        /// 从索引键定义获取列名
        /// </summary>
        private string GetIndexColumns(object key)
        {
            // 这里应该实现从索引键定义获取列名的逻辑
            // 为简化，返回一个占位符
            return "column";
        }

        /// <summary>
        /// 从索引键定义获取索引类型
        /// </summary>
        private string GetIndexType(object key)
        {
            // 这里应该实现从索引键定义获取索引类型的逻辑
            // 为简化，返回一个占位符
            return "USING btree";
        }
        #endregion

    }
}
#endif