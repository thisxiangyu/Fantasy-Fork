using Fantasy.Async;
using Fantasy.Database;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    [DbSet(Name = "FantasyDbSetExample", WithNamespace = true)]
    public class ExampleRoot : Entity, IMultiAppended, IDbSet
    {
        public DbSetOptions DbSetOpts => new() { Name = "FantasyDbSetExample", WithNamespace = true };

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
            var b2 = componentA.GetOrAddComponent<ComponentB>();
            var c2 = componentA.GetOrAddComponent<ComponentC_AsEmbeddDoc>();
            var componentC = c2.GetOrAddComponent<ComponentC_AsEmbeddDoc>();
            var grandChild = componentC.GetOrAddComponent<Grandchild_AsDoc>();

            var a2 = child01.GetOrAddComponent<ComponentA>();
            var b3 = child02.GetOrAddComponent<ComponentB>();

            var a3 = grandChild.GetOrAddComponent<ComponentA>();
            var b4 = grandChild.GetOrAddComponent<ComponentB>();
            var c3 = grandChild.GetOrAddComponent<ComponentC_AsEmbeddDoc>();

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
                        }
                        break;
                    }
                case TestWhat.Insert:
                    {
                        if (db is PostgreSQL pgSQL)
                        {
                            using var scope = pgSQL.Use(out PgSession? dbSession);

                            if (dbSession == null)
                                throw new("Failed to use dbSession.");

                            //测试事务性
                            using var transaction = await dbSession.Database.BeginTransactionAsync();
                            try
                            {
                                //await dbSession.InsertBatch(componentA.ForEachMulti<Child>());
                                //await dbSession.Insert(this);
                                await dbSession.Insert(componentA);
                                //await dbSession.Insert(componentB);
                                await dbSession.Insert(grandChild);
                                //await dbSession.Insert(a2);
                                //await dbSession.Insert(a3);
                                //await dbSession.Insert(b2);
                                //await dbSession.Insert(b3);
                                //await dbSession.Insert(b4);
                                await transaction.CommitAsync();   // 提交事务
                            }
                            catch
                            {
                                Log.Warning("--------------事务回滚了--------------");
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                        break;
                    }
                case TestWhat.Save:
                    {

                        break;
                    }
                case TestWhat.Query:
                    {
                        if(db is PostgreSQL pgSQL)
                        {
                            Log.Debug("--------------------PgSQL API测试开始--------------------");
                            using var scope = pgSQL.Use(out PgSession? dbSession);
                            if (dbSession == null)
                                throw new("Failed to use dbSession.");

                            var notExist = await dbSession.Query<ComponentC_AsEmbeddDoc>(id: 10000); //测试失败查询

                            if (notExist == null)
                                Log.Info($"id:{10000} ComponentC 不存在.");
                            else
                                Log.Warning("测试失败查询异常.");

                            var root = await dbSession.Query<ExampleRoot>(id: 465195535759048706); //成功查询                            
                             Log.Info($"ExampleRoot :{root.Id} Scene:{root.Scene.SceneConfigId}.");

                            var component_a =  (await dbSession.QueryAppend<ComponentA>(root)).FirstOrDefault(); //查询且附加
                            Log.Info($"component_a :{component_a!.Id} 已查到, 且附加到了 ExampleRoot :{root.Id}");

                            var children = await dbSession.QueryAppend<Child>(component_a!); //多子实体查询
                            foreach (var child in children)
                            {
                                var result1 = await dbSession.QueryAppend<ComponentA, ComponentB>(child); //双重查询且附加
                                Log.Info($"查到 component_a 的 child {child.Id} 有 {result1.Item1.Count()}个 ComponentA 和 {result1.Item2.Count()}个 ComponentB");
                            }
                            //var result2 = await dbSession.QueryAppend<Child,ComponentB,ComponentC_EmbeddDoc>(component_a!); //三重查询且附加
                            Log.Debug("--------------------PgSQL API测试结束--------------------");
                        }
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
