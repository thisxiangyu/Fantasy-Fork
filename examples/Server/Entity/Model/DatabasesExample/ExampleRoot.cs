using Fantasy.Async;
using Fantasy.Database;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    [DbSet(Name = "FantasyDbSetExample", Relationship = ToParentIs.JustLinking, WithNamespace = true)]
    public class ExampleRoot : Entity, IMultiAppended
    {
        public int TestIntField { get; set; }

        public int TestStringField { get; set; }

        [NotMapped]
        public int NotMapped { get; set; }

        public async FTask StartTest<T>(TestWhat testItem,int dutyId)where T:class,IDatabase 
        {
            var componentA = GetOrAddComponent<ComponentA>();
            var componentB = GetOrAddComponent<ComponentB>();

            var child01 = componentA.GetOrAddComponent<Child>();
            var child02 = componentA.GetOrAddComponent<Child>();
            var child03 = componentA.GetOrAddComponent<Child>();

            child01.GetOrAddComponent<ComponentA>();
            child02.GetOrAddComponent<ComponentB>();

            var componentC = child03.GetOrAddComponent<ComponentC>();
            var grandChild = componentC.GetOrAddComponent<Grandchild>();
            grandChild.GetOrAddComponent<ComponentA>();
            grandChild.GetOrAddComponent<ComponentB>();
            grandChild.GetOrAddComponent<ComponentC>();

            Log.Debug("[FantasyDbSet] 测试: 实体树已构造,");
            Log.Debug("\r\n        ///     Root\r\n        ///     ├─ ComponentA\r\n        ///     │  ├─ Child01\r\n        ///     │  │  └─ ComponentA\r\n        ///     │  ├─ Child02\r\n        ///     │  │  └─ ComponentB\r\n        ///     │  └─ Child03\r\n        ///     │     └─ ComponentC\r\n        ///     │        └─ Grandchild\r\n        ///     │           ├─ ComponentA\r\n        ///     │           ├─ ComponentB\r\n        ///     │           └─ ComponentC\r\n        ///     └─ ComponentB");

            var db = Scene.World.GetDatabase<T>(dutyId);

            if (db == null)
            {
                Log.Error($"{typeof(T)}不存在,无法进行测试");
                return;
            }

            switch (testItem)
            {
                case TestWhat.FastDeploy:
                    {
                        await db.FastDeploy();

                        if(db is PostgreSQL pgSQL)
                        {
                            using var scope = pgSQL.Use(out PgSession? pgSession);
                            if (pgSession == null)
                                throw new("Failed to use dbSession.");
                            //var childEntityType = pgSession.Model.FindEntityType(typeof(ComponentB))!;
                            //var fkProperty = childEntityType.FindProperty("ExampleRoot")!;
                            //var parentEntityType = pgSession.Model.FindEntityType(typeof(ExampleRoot))!;
                            //var principalKey = parentEntityType.FindPrimaryKey()!;
                            //// 测试确认一下FK存在性
                            //var fk = childEntityType.FindForeignKey(fkProperty, principalKey, parentEntityType);
                            //Log.Debug(fk != null ? "测试确认一下FK存在性: ForeignKeyByParentHash exists in model" : "测试确认一下FK存在性: ForeignKeyByParentHash not found in model");
                        }
                        break;
                    }
                case TestWhat.Insert:
                    {
                        using var scope = db.Use(out IDbSession? dbSession);

                        if (dbSession == null)
                            throw new("Failed to use dbSession.");

                        await dbSession.Insert(this);
                        await dbSession.Insert(componentA);
                        await dbSession.Insert(componentB);
                        await dbSession.Insert(componentC);

                        break;
                    }
                case TestWhat.Save:
                    {

                        break;
                    }
                case TestWhat.Query:
                    {

                        break;
                    }
                case TestWhat.QueryWithIndexes:
                    {

                        break;
                    }
                case TestWhat.JoinQuery:
                    {

                        break;
                    }
            }
        }

        public void TestAPI<T>(int dutyId) where T : class, IDatabase
        {
            var db = Scene.World.GetDatabase<T>(dutyId);

            if (db == null)
            {
                Log.Error($"{typeof(T)}不存在,无法进行测试");
                return;
            }
            if (db is MongoDb mongo)
            {
                //mongo.开始测试(限流锁测试.信号量锁住Id).Coroutine();
                mongo.开始测试(限流锁测试.信号量锁).Coroutine();
                mongo.开始测试(限流锁测试.随机数锁).Coroutine();
                //mongo.开始信号量锁有效性测试().Coroutine();
            }

            if (db is PostgreSQL pgSQL)
            {
                Log.Debug("《 PgSQL API 大测试》");

                //using (pgSQL.Use(out IDbSession? session, useSessionFromPool: false))
                //{
                //    if (session == null)
                //        Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                //    else
                //        Log.Debug("已创建非池化的会话");
                //}

                //Log.Warning("↑看看非池化的会话有没有Dispose呢?");

                using (pgSQL.Use(out IDbSession? session, useSessionFromPool: true))
                {
                    if (session == null)
                        Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                    else
                        Log.Debug("已创建池化的会话");
                }

                Log.Warning("↑看看池化的会话有没有Dispose呢?");

                //using (pgSQL.Use(out PgSession? session, useSessionFromPool: false))
                //{
                //    if (session == null)
                //        Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                //    else
                //        Log.Debug("已创建PgSession强类型非池化会话");
                //}

                //Log.Warning("↑看看PgSession强类型非池化的会话有没有Dispose呢?");

                //using (pgSQL.Use(out PgSession? session, useSessionFromPool: true))
                //{
                //    if (session == null)
                //        Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                //    else
                //        Log.Debug("已创建池化的PgSession强类型会话");
                //}

                //Log.Warning("↑看看PgSession池化的强类型的会话有没有Dispose呢?");

                //await pgSQL.Invoke(async (session) =>
                //{
                //    if (session == null)
                //        Log.Error($"( Failed to connect to PgSQL logic-database)\n ");
                //    else
                //        Log.Debug("Invoke测试(Session默认池化)");
                //    await FTask.CompletedTask;
                //});
                //Log.Warning("↑看看Invoke的PgSession(默认池化)有没有Dispose呢?");
            }
            
        }

        public enum TestWhat
        {
            FastDeploy,
            Init,
            Insert,
            Save,
            Query,
            QueryWithIndexes,
            JoinQuery
        }
    }
}
