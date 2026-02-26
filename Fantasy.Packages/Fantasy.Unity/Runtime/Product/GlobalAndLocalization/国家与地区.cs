using System;
using System.Collections;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using MemoryPack;

#if FANTASY_UNITY
using UnityEngine.Networking;
#endif

namespace Fantasy.GlobalAndLocalization
{
    [DbSet(IsEmbedded = true)]
    [MemoryPackable]
    public partial class 国家与地区相关信息 : Entity
    {
        /// <summary>
        /// 据<see cref="区域码"/>
        /// </summary>
        public string 当前所在国家或地区 { get; set; } = "Unknown";
        public string 当前所选语言码 { get; set; } = "en";

#if FANTASY_UNITY
        public IEnumerator 快速获取当前IP区域代码(Action<string> callback)
        {
            using var www = UnityWebRequest.Get("https://ipapi.co/country/");
            www.timeout = 5;

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // 成功后，通过回调把区域代码传出去
                callback?.Invoke(www.downloadHandler.text.Trim());
            }
            else
            {
                Log.Error(www.error);
                callback?.Invoke("UNKNOWN");
            }
        }
#endif
#if FANTASY_NET
        //TODO 通过部署 MaxMind GeoLite2 City 来查询客户端请求的具体IP位置
        //TODO MaxMind GeoLite2 City 每隔1周时间需要在服务器热更新/冷更新一次, 因为IP位置常常会变动

#endif

    }
}