global using MicrosoftJsonSerializer = System.Text.Json.JsonSerializer;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Fantasy.Entitas;
using Fantasy.Entitas.TypeMeta;
using Newtonsoft.Json;
using static Fantasy.Helper.JsonHelper;
#pragma warning disable CS8603

namespace Fantasy.Helper
{
    /// <summary>
    /// 提供用不同库安全操作 JSON 数据的辅助方法或拓展方法。
    /// </summary>
    public static partial class JsonHelper
    {
        /// <summary>
        /// Json库类型
        /// </summary>
        public enum Library
        {
            /// <summary>
            /// .NET自带, 微软提供的Json库, 性能通常略占优势
            /// </summary>
            Microsoft,
            /// <summary>
            /// 第三方开发者Newtonsoft提供的Json库  
            /// </summary>
            Newtonsoft
        }

        /// <summary>
        /// 标识符
        /// </summary>
        private class Mark
        {
            /// <summary>
            /// 代表 微软
            /// </summary>
            public const string M = "M";
            /// <summary>
            /// 代表 Newtonsoft
            /// </summary>
            public const string N = "N";
        }

        /// <summary>
        /// Json额外元属性名
        /// </summary>
        public class MetaPropertyStr
        {
            /// <summary>
            /// 序列化库
            /// </summary>
            public const string Lib = "$L";
            /// <summary>
            /// 数据存放处
            /// </summary>
            public const string Data = "$D";
            /// <summary>
            /// 类型标识
            /// </summary>
            public const string Type = "$T";
        }

        /// <summary>
        /// Json序列化器统一的控制选项。
        /// </summary>
        public struct SerializerSettings
        {
            /// <summary>
            /// 所选用库
            /// </summary>
            public Library Library = Library.Microsoft;
            /// <summary>
            /// 是否把类型信息写入Json
            /// </summary>
            public bool WriteType = false;
            /// <summary>
            /// 是否采用缩进格式
            /// </summary>
            public bool IsIndented = true;

            // ... NOTE: 除了以上, 未来还可以拓展

            /// <summary>
            /// 构造函数
            /// </summary>
            public SerializerSettings()
            {
            }
        }

        #region SettingsCache

        // ** 这里缓存采用 List 而不是 HashSet, 因为元素少的情况下遍历列表取值更快 **//
        private static readonly List<(SerializerSettings, JsonSerializerOptions)> _serializerSettingsCache_M = new();
        private static readonly List<(SerializerSettings, JsonSerializerSettings)> _serializerSettingsCache_N = new();

        // ** 线程安全缓存 **//
        private static readonly List<(SerializerSettings, JsonSerializerOptions)> _lockedCache_M = new();
        private static readonly List<(SerializerSettings, JsonSerializerSettings)> _lockedCache_N = new();
        private static readonly object _lock_M = new object();
        private static readonly object _lock_N = new object();

        // Newtonsoft序列化器默认设置
        private readonly static JsonSerializerSettings _defaultSettings = new() {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };

        // Microsoft序列化器默认设置 
        private readonly static JsonSerializerOptions _defaultOptions = new()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        /// <summary>
        /// 为微软的Json序列化器配置自定义的<see cref="PolymorphismResolver"/>作为高级控制项, 使其具有多态鉴别能力。
        /// </summary>
        private static readonly IJsonTypeInfoResolver ResolverWithPolymorphism = JsonTypeInfoResolver.Combine(
                            new PolymorphismResolver()
                        );

        private static JsonSerializerOptions MakeMicrosoftOptions(SerializerSettings settings, bool threadSafe = false) {

            if (!threadSafe)
            {
                for (int i = _serializerSettingsCache_M.Count - 1; i >= 0; i--)
                {
                    var (item1, item2) = _serializerSettingsCache_M[i];
                    if (!item1.Equals(settings)) 
                        continue;

                    if (item2 == null)
                        _serializerSettingsCache_M.RemoveAt(i);
                    else return item2;
                }
            }
            else lock (_lock_M)
            {
                for (int i = _lockedCache_M.Count - 1; i >= 0; i--)
                {
                    var (item1, item2) = _lockedCache_M[i];
                    if (!item1.Equals(settings)) 
                         continue;

                    if (item2 == null)
                        _lockedCache_M.RemoveAt(i);
                    else return item2;
                }
            }

            var opt = new JsonSerializerOptions
            {
                WriteIndented = settings.IsIndented,
                ReferenceHandler = ReferenceHandler.Preserve,
                TypeInfoResolver = settings.WriteType ? ResolverWithPolymorphism : null
            };

            if (!threadSafe)
            {
                _serializerSettingsCache_M.Add((settings, opt));
            }
            else lock (_lock_M)
            {
                _lockedCache_M.Add((settings, opt));
            }

            return opt;
        }

        private static JsonSerializerSettings MakeNewtonsoftSettings(SerializerSettings settings, bool threadSafe = false)
        {
            if (!threadSafe)
            {
                for (int i = _serializerSettingsCache_N.Count - 1; i >= 0; i--)
                {
                    var (item1, item2) = _serializerSettingsCache_N[i];

                    if (!item1.Equals(settings)) 
                        continue;

                    if (item2 == null)
                        _serializerSettingsCache_N.RemoveAt(i);
                    else return item2; //直接返回已缓存的设置
                }
            }
            else lock (_lock_N) 
            {
                for (int i = _lockedCache_N.Count - 1; i >= 0; i--)
                {
                    var (item1, item2) = _lockedCache_N[i];

                    if (!item1.Equals(settings)) 
                        continue;

                    if (item2 == null)
                        _lockedCache_N.RemoveAt(i);
                    else return item2; //直接返回已缓存的设置
                }
            }

            var setting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = settings.WriteType ? TypeNameHandling.All : TypeNameHandling.None,
                Formatting = settings.IsIndented ? Formatting.Indented : Formatting.None
            };

            // 添加到缓存
            if (!threadSafe)
            {
                _serializerSettingsCache_N.Add((settings, setting));            
            }
            else lock (_lock_N) {
               _lockedCache_N.Add((settings, setting));
            }

            return setting;
        }

        #endregion


        /// <summary>
        /// 一个Json包装器, 用于包装额外的可解析标头或标尾信息到框架序列化的Json中
        /// </summary>
        public class Wrapper<T>
        {
            /// <summary>
            /// 库类型
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName(MetaPropertyStr.Lib)]
            [Newtonsoft.Json.JsonProperty(MetaPropertyStr.Lib)]
            public string? LibraryMark { get; set; }

            // ... NOTE : 未来可以拓展

            /// <summary>
            /// 数据存放处
            /// </summary>
            [System.Text.Json.Serialization.JsonPropertyName(MetaPropertyStr.Data)]
            [Newtonsoft.Json.JsonProperty(MetaPropertyStr.Data)]
            public T? Data { get; set; }
        }

        /// <summary>
        /// 将对象序列化为 JSON 字符串。允许传入序列化器的相关设置。
        /// </summary>
        /// <typeparam name="T">要序列化的对象类型。</typeparam>
        /// <param name="t">要序列化的对象。</param>
        /// <param name="settings">序列化器设置</param>
        /// <param name="isCacheThreadSafe">将缓存设置为线程安全, 默认为 false ;如果开启线程安全, 自动加锁会导致性能略微降低. </param>
        public static string ToJson<T>(this T t, SerializerSettings? settings = null, bool isCacheThreadSafe = false) 
        {
            // 默认直接用Newton的库
            if (settings==null)
                return JsonConvert.SerializeObject(t, _defaultSettings);

            // 创建包装器
            var wrapper = new Wrapper<T>
            {
                LibraryMark = default,
                Data = t
            };

            string json = string.Empty;
            var lib = settings.Value.Library;
            wrapper.LibraryMark = lib == Library.Microsoft ? Mark.M : Mark.N;

            switch (lib)
            {
                case Library.Microsoft:
                    json = MicrosoftJsonSerializer.Serialize(wrapper, MakeMicrosoftOptions(settings.Value, isCacheThreadSafe));
                    break;

                case Library.Newtonsoft:
                    json = JsonConvert.SerializeObject(wrapper, MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe));
                    break;

                default:
                    throw new Exception("Unexpected: librarySelection is Unknown.");
            }

            return json;
        }

        /// <summary>
        /// 反序列化 JSON 字符串为指定类型的对象。
        /// </summary>
        /// <param name="json">要反序列化的 JSON 字符串。</param>
        /// <param name="type">目标对象的类型。</param>    
        /// <param name="settings">序列化器设置</param>
        /// <param name="isCacheThreadSafe">将缓存设置为线程安全, 默认为 false ;如果开启线程安全, 自动加锁会导致性能略微降低. </param>
        /// <returns>反序列化后的对象。</returns>
        public static object Deserialize(this string json, Type type, SerializerSettings? settings = null, bool isCacheThreadSafe = false)
        {
            // 探测是不是 Wrapper 结构
            settings ??= new SerializerSettings();
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonElement LibMark_Element = default;
            JsonElement Data_Element = default;

            bool isWrapped = root.ValueKind == JsonValueKind.Object &&
                             root.TryGetProperty(MetaPropertyStr.Lib, out LibMark_Element) &&
                             root.TryGetProperty(MetaPropertyStr.Data, out Data_Element);

            if (isWrapped)
            {
                string? libraryMark = LibMark_Element.GetString();
                Type wrapperType = typeof(Wrapper<>).MakeGenericType(type); // 构造闭合泛型 Wrapper<T> 类型

                switch(libraryMark)
                {
                    case Mark.M:  //使用微软库
                        {
                            JsonSerializerOptions options = MakeMicrosoftOptions(settings.Value, isCacheThreadSafe);
                            dynamic? wrapper = MicrosoftJsonSerializer.Deserialize(json, wrapperType, options);
                            return wrapper?.Data;
                        }
                    case Mark.N:  //使用Newtonsoft库
                        {
                            JsonSerializerSettings options = MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe);
                            dynamic? wrapper = JsonConvert.DeserializeObject(json, wrapperType, options);
                            return wrapper?.Data;
                        }
                    default: throw new Exception("Unexpected: Detected unknown Json library mark. Deserialize failed. ");
                }
            }
            else  // --- 处理未包装的JSON  ---
            {
                JsonSerializerSettings options = MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe);

                try
                {
                    return JsonConvert.DeserializeObject(json, type, MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe));
                }
                catch (Exception ex)
                {
                    // 如果 Newtonsoft 失败，尝试改用 Microsoft
                    try
                    {
                        return MicrosoftJsonSerializer.Deserialize(json, type, MakeMicrosoftOptions(settings.Value, isCacheThreadSafe));
                    }
                    catch (Exception)
                    {
                        // 两个都失败
                        throw
                        new Exception($"Deserialize Failed for {type.Name}. Msg: \n {ex.Message}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// 反序列化 JSON 字符串为指定类型的对象。
        /// </summary>
        /// <typeparam name="T">目标对象的类型。</typeparam>
        /// <param name="json">要反序列化的 JSON 字符串。</param>
        /// <param name="settings">序列化器设置</param>
        /// <param name="isCacheThreadSafe">将缓存设置为线程安全, 默认为 false ;如果开启线程安全, 自动加锁会导致性能略微降低. </param>
        /// <returns>反序列化后的对象。</returns>
        public static T Deserialize<T>(this string json, SerializerSettings? settings = null, bool isCacheThreadSafe = false)
        {
            return (T)Deserialize(json, typeof(T), settings, isCacheThreadSafe);
        }

        /// <summary>
        /// 克隆对象，通过将对象序列化为 JSON，然后再进行反序列化。
        /// </summary>
        /// <typeparam name="T">要克隆的对象类型。</typeparam>
        /// <param name="t">要克隆的对象。</param>
        /// <returns>克隆后的对象。</returns>
        public static T Clone<T>(T t)
        {
            return t.ToJson().Deserialize<T>();
        }
    }

    /// <summary>
    /// Json多态鉴别配置器。这个类是针对采用微软的Json库的情况下序列化和反序列化的类型写入拓展，
    /// 用于将一个"<see langword="T"/>"字段注入Json, 以记录对象的真实类型。
    /// <para>
    /// TODO : 对于<see cref="Entity"/>类型, 将会把<see cref="Entity.TypeHashCode"/>以<see langword="uint"/>的方式写出, 
    /// 其它类型将会把<see cref="Type.FullName"/>以<see langword="string"/>的形式写出。
    /// </para>
    /// <para>
    /// 支持<see cref="List{T}"/>和<see cref="Array"/>以及它们派生数据容器, 在转为Json时, 即便元素是非基类的实例, 也不会丢弃每个元素真实的<see cref="Type"/>。
    /// </para>
    /// </summary>
    internal class PolymorphismResolver : DefaultJsonTypeInfoResolver
    {
        // 假设您已经通过反射得到了所有继承自 Entity 的派生类型
        private static readonly IReadOnlyList<Type> KnownDerivedTypes = new List<Type>
        {
            //....
        };

        /// <summary>
        /// 覆写GetTypeInfo
        /// </summary>
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo info = base.GetTypeInfo(type, options);

            info.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = MetaPropertyStr.Type,
                IgnoreUnrecognizedTypeDiscriminators = false, // 这个设置的效果: 不识别的类型会直接抛出异常, 而不是忽略掉。
            };

            // 注册所有已知的派生类型
            foreach (var derivedType in KnownDerivedTypes)
            {
                if (derivedType.FullName == null)
                    throw new Exception("Entity type`s FullName is Null.");

                //用FullName就可以, 但用long的HashCode就不支持; 从性能考虑, 后续可以考虑把框架类型码改成uint,
                //再优化这里。暂时就用FullName了。
                string fullName = derivedType.FullName;
                //long typeHashCode = TypeHashCache.GetHashCode(info.Type);

                info.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(derivedType, fullName)
                );
            }

           return info;
        }
    }
}