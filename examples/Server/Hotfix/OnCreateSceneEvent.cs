using Fantasy.Assembly;
using Fantasy.Async;
using Fantasy.Database;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;
using static Fantasy.ExampleRoot;

namespace Fantasy;

public sealed class SaveEntity : Entity
{

}

public sealed class SubSceneTestComponent : Entity
{
    public override void Dispose()
    {
        Log.Debug("销毁SubScene下的SubSceneTestComponent");
        base.Dispose();
    }
}

public sealed class SubSceneTestComponentAwakeSystem : AwakeSystem<SubSceneTestComponent>
{
    protected override void Awake(SubSceneTestComponent self)
    {
        Log.Debug("SubSceneTestComponentAwakeSystem");
    }
}

public sealed class OnCreateSceneEvent : AsyncEventSystem<OnCreateScene>
{
    private static long _addressableSceneRunTimeId;

    /// <summary>
    /// Handles the OnCreateScene event.
    /// </summary> 
    /// <param name="self">The OnCreateScene object.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;

        await FTask.CompletedTask;

        switch (scene.SceneType)
        {
            case 6666:
                {
                    break;
                }
            case SceneType.Addressable:
                {
                    _addressableSceneRunTimeId = scene.RuntimeId;
                    break;
                }
            case SceneType.Map:
                {
                    Log.Debug($"Map Scene  SceneRuntimeId:{scene.RuntimeId}");
                    break;
                }
            case SceneType.Chat:
                {
                    break;
                }
            case SceneType.Gate:
            {              
                // 执行自定义系统
                var testCustomSystemComponent = scene.AddComponent<TestCustomSystemComponent>();
                // scene.EntityComponent.CustomSystem(testCustomSystemComponent, CustomSystemType.RunSystem);
                // // 测试配置表
                // var instanceList = UnitConfigData.Instance.List;
                // var unitConfig = instanceList[0];
                // Log.Debug(instanceList[0].Dic[1]);
                break;
            }
        }

        //测试FantasyDbSet
        if (scene.SceneConfigId == 1001)
        {
            var TestFantasyDbSetRoot = scene.AddComponent<ExampleRoot>();
            await TestFantasyDbSetRoot.StartTest<PostgreSQL>(TestWhat.FastDeploy, dutyId: 0);
            //await TestFantasyDbSetRoot.StartTest<PostgreSQL>(TestWhat.Insert, dutyId: 0);
            //await TestFantasyDbSetRoot.StartTest<PostgreSQL>(TestWhat.Query, dutyId: 0);

            //TestFantasyDbSetRoot.StartTest<MongoDb>(TestWhat.FastDeploy, dutyId: 2).Coroutine();
            //TestFantasyDbSetRoot.StartTest<MongoDb>(TestWhat.Insert, dutyId: 2).Coroutine();
            //TestFantasyDbSetRoot.TestAPI<MongoDb>(2);
        }
    }
}