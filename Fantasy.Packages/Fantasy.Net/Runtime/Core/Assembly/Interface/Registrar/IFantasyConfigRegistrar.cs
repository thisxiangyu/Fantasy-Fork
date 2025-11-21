#if FANTASY_NET
using System.Collections.Generic;
namespace Fantasy.Assembly;

/// <summary>
/// 启服配置注册接口
/// </summary>
public interface IFantasyConfigRegistrar
{
    /// <summary>
    /// SceneType注册
    /// </summary>
    /// <returns></returns>
    Dictionary<string, int> GetSceneTypeDictionary();
}
#endif