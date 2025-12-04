using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;
using Fantasy.Helper;
using static Fantasy.Helper.JsonHelper;
using Newtonsoft.Json;

#pragma warning disable CS8632

[Serializable]
public class JSONTextClass
{
    public string NameField = "I am A";
}

public class JsonHelperPK : MonoBehaviour
{

    private static Scene scene;

    void Start()
    {
        // Unity Vs Newtonsoft 性能测试
        TestObjectSerialization(true).Coroutine();
    }


    #region Json序列化-反序列化测试
    public static async FTask TestObjectSerialization(bool logJson = false)
    {
        await Fantasy.Platform.Unity.Entry.Initialize();
        scene ??= await Scene.Create(SceneRuntimeMode.MainThread);

        int loop = 888; // 循环次数
        Stopwatch sw = new Stopwatch();

        // ---------------- NewtonSoft ----------------
        var serializerSettingsNewtonsoft = new JsonSettings
        {
            Library = Library.Newtonsoft,
            IsIndented = true,
            WriteTypeWhenNecessary = true,
            NoCycles = true,
            NoNull = true
        };

        double totalSerializeNewtonsoftSingle = 0;
        double totalDeserializeNewtonsoftSingle = 0;
        double totalSerializeNewtonsoftList = 0;
        double totalDeserializeNewtonsoftList = 0;

        string? jsonSingle = null;
        string? jsonList = null;

        Log.Info($"----------------------- NewtonsoftJson {loop}次执行---------------------------");

        for (int i = 0; i < loop; i++)
        {
            // 每次循环都创建新实体，避免重复使用同一对象
            var tObject0 = new JSONTextClass();
            var tObject1 = new JSONTextClass();
            var tObject2 = new JSONTextClass();
            List<JSONTextClass> tObjectList = new() { tObject0, tObject1, tObject2 };

            // 单个对象序列化
            sw.Restart();
            jsonSingle = tObject0.ToJson(serializerSettingsNewtonsoft);
            sw.Stop();
            totalSerializeNewtonsoftSingle += sw.Elapsed.TotalMilliseconds;

            // 单个对象反序列化
            sw.Restart();
            var objSingle = jsonSingle.Deserialize<JSONTextClass>(serializerSettingsNewtonsoft);
            sw.Stop();
            totalDeserializeNewtonsoftSingle += sw.Elapsed.TotalMilliseconds;

            // 列表序列化
            sw.Restart();
            jsonList = tObjectList.ToJson(serializerSettingsNewtonsoft);
            sw.Stop();
            totalSerializeNewtonsoftList += sw.Elapsed.TotalMilliseconds;

            // 列表反序列化
            sw.Restart();
            var objList = jsonList.Deserialize<List<JSONTextClass>>(serializerSettingsNewtonsoft);
            sw.Stop();
            totalDeserializeNewtonsoftList += sw.Elapsed.TotalMilliseconds;

            if (i == 0 && logJson)
            {
                Log.Info("[Newtonsoft] 处理单个对象 JSON:\n" + jsonSingle);
                Log.Info("[Newtonsoft] 处理列表 JSON:\n" + jsonList);
            }
        }

        Log.Info($"[Newtonsoft] 平均单个对象序列化耗时: {totalSerializeNewtonsoftSingle / loop:F4} ms 总耗时 {Math.Round(totalSerializeNewtonsoftSingle, 2)} ms");
        Log.Info($"[Newtonsoft] 平均单个对象反序列化耗时: {totalDeserializeNewtonsoftSingle / loop:F4} ms 总耗时 {Math.Round(totalDeserializeNewtonsoftSingle, 2)} ms");
        Log.Info($"[Newtonsoft] 平均列表序列化耗时: {totalSerializeNewtonsoftList / loop:F4} ms 总耗时 {Math.Round(totalSerializeNewtonsoftList, 2)} ms");
        Log.Info($"[Newtonsoft] 平均列表反序列化耗时: {totalDeserializeNewtonsoftList / loop:F4} ms 总耗时 {Math.Round(totalDeserializeNewtonsoftList, 2)} ms");

        Log.Info($"----------------------- UnityJson {loop}次执行---------------------------");

        // ---------------- Unity ----------------
        var serializerSettingsUnity = new JsonSettings
        {
            Library = Library.UnityJson,
            IsIndented = true
        };

        double totalSerializeUnitySingle = 0;
        double totalDeserializeUnitySingle = 0;
        double totalSerializeUnityList = 0;
        double totalDeserializeUnityList = 0;

        for (int i = 0; i < loop; i++)
        {
            // 每次循环都创建新实体
            var tObject0 = new JSONTextClass();
            var tObject1 = new JSONTextClass();
            var tObject2 = new JSONTextClass();
            List<JSONTextClass> tObjectList = new() { tObject0, tObject1, tObject2 };

            // 单个对象序列化
            sw.Restart();
            jsonSingle = tObject0.ToJson(serializerSettingsUnity);
            sw.Stop();
            totalSerializeUnitySingle += sw.Elapsed.TotalMilliseconds;

            // 单个对象反序列化
            sw.Restart();
            var objSingle = jsonSingle.Deserialize<JSONTextClass>();
            sw.Stop();
            totalDeserializeUnitySingle += sw.Elapsed.TotalMilliseconds;

            // 列表序列化
            sw.Restart();
            jsonList = tObjectList.ToJson(serializerSettingsUnity);
            sw.Stop();
            totalSerializeUnityList += sw.Elapsed.TotalMilliseconds;

            // 列表反序列化
            sw.Restart();
            var objList = jsonList.Deserialize<List<JSONTextClass>>();
            sw.Stop();
            totalDeserializeUnityList += sw.Elapsed.TotalMilliseconds;

            if (i == 0 && logJson)
            {
                Log.Info("[Unity] 处理单个对象 JSON:\n"+ jsonSingle);
                Log.Info("[Unity] 处理列表 JSON:\n" + jsonList);
            }
        }

        Log.Info($"[Unity] 平均单个对象序列化耗时: {totalSerializeUnitySingle / loop:F4} ms 总耗时 {Math.Round(totalSerializeUnitySingle, 2)} ms");
        Log.Info($"[Unity] 平均单个对象反序列化耗时: {totalDeserializeUnitySingle / loop:F4} ms 总耗时 {Math.Round(totalDeserializeUnitySingle, 2)} ms");
        Log.Info($"[Unity] 平均列表序列化耗时: {totalSerializeUnityList / loop:F4} ms 总耗时 {Math.Round(totalSerializeUnityList, 2)} ms");
        Log.Info($"[Unity] 平均列表反序列化耗时: {totalDeserializeUnityList / loop:F4} ms 总耗时 {Math.Round(totalDeserializeUnityList, 2)} ms");
    }
    #endregion
}
