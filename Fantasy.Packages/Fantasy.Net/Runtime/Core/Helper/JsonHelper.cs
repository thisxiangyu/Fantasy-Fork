#if FANTASY_NET
using MicrosoftJsonSerializer = System.Text.Json.JsonSerializer;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
#endif
#if FANTASY_UNITY
using UnityEngine;
using Newtonsoft.Json.Linq;
#endif
using Fantasy.Assembly;
using Fantasy.Entitas;
using Newtonsoft.Json;
using static Fantasy.Helper.JsonHelper;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Buffers;
using System.Runtime.CompilerServices;

#pragma warning disable CS8632
#pragma warning disable CS8603

namespace Fantasy.Helper
{
    public enum DetectMode
    {
        Auto = 0,
        MustBeWrapper = 1,
        MustBeNormal = 2
    }

    /// <summary>
    /// 一个Json包装器, 用于包装额外可解析的标头或标尾信息到框架序列化的Json中
    /// </summary>
    [Serializable]
    public class JsonWrapper<T> : IDataAccessible
    {
        /// <summary>
        /// 表示序列化库提供方
        /// </summary>
#if FANTASY_NET
        [System.Text.Json.Serialization.JsonPropertyName(MetaPropertyStr.L)]
#endif
        [Newtonsoft.Json.JsonProperty(MetaPropertyStr.L)]
#if FANTASY_NET
        public string? L { get; set; }
#endif
#if FANTASY_UNITY
        public string? L;
#endif
        /// <summary>
        /// 表示数据存放处
        /// </summary>
#if FANTASY_NET
        [System.Text.Json.Serialization.JsonPropertyName(MetaPropertyStr.D)]
#endif
        [Newtonsoft.Json.JsonProperty(MetaPropertyStr.D)]
#if FANTASY_NET
        public T? Data { get; set; }
#endif
#if FANTASY_UNITY
        public T? Data;
#endif

        /// <summary>
        /// 访问数据, 以object返回
        /// </summary>
        public object? AccessData() => Data;
    }

    /// <summary>
    /// 数据可访问接口
    /// </summary>
    public interface IDataAccessible
    {
        /// 访问数据, 以object返回
        object? AccessData();
    }

    /// <summary>
    /// Json序列化器的控制选项。Newtonsoft 和 Microsoft的库通用。
    /// <para>
    /// 注 : Unity仅支持缩进, 不支持其余设置。
    /// </para>
    /// </summary>
    public struct JsonSettings : IEquatable<JsonSettings>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public JsonSettings(
           Library library = Library.Newtonsoft,
           bool isIndented = true,
           bool writeTypeWhenNecessary = true,
           bool noCycles = false,
           bool noNull = true)
        {
            Library = library;
            IsIndented = isIndented;
            WriteTypeWhenNecessary = writeTypeWhenNecessary;
            NoCycles = noCycles;
            NoNull = noNull;
        }
        /// <summary>
        /// 选择序列化库。
        /// </summary>
        public Library Library;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetLibraryMark()
        {
            switch (Library)
            {
                case Library.Newtonsoft:
                    return Mark.N;
#if FANTASY_NET
                case Library.Microsoft:
                    return Mark.M;
#endif
#if FANTASY_UNITY
                case Library.UnityJson:
                    return Mark.U;
#endif
                default: throw new("Unexpected: Unknown");
            }
        }
        /// <summary>
        /// 采用缩进格式。
        /// </summary>
        public bool IsIndented;
        /// <summary>
        /// 必要时把类型信息写到Json当中。
        /// <para>
        /// 当派生类实例以基类的形式序列化时会生效, 典型的情况是List序列化时保存了各种不同类型的实体，需要记录真实类型，才能正确反序列化。
        /// </para>
        /// <para>
        ///  ( 注: Newtonsoft库支持写出任意类型; Microsoft库开启这个后,框架仅默认支持派生自<see cref="Entity"/>的情况, 如需拓展自定义派生类型写出, 需自行实现微软的Json库多态配置, 框架不予额外支持。)
        /// </para>
        /// </summary>
        public bool WriteTypeWhenNecessary;
        /// <summary>
        /// 禁用循环引用。
        /// </summary>
        public bool NoCycles;
        /// <summary>
        /// 关闭Null值写出。
        /// </summary>
        public bool NoNull;

        // ... NOTE: 除了以上, 未来可拓展


        public bool Equals(JsonSettings other)
        {
            // 对比字段
            return Library == other.Library &&
                   IsIndented == other.IsIndented &&
                   WriteTypeWhenNecessary == other.WriteTypeWhenNecessary &&
                   NoCycles == other.NoCycles &&
                   NoNull == other.NoNull;
        }

        public override bool Equals(object? obj) => obj is JsonSettings other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(Library, IsIndented, WriteTypeWhenNecessary, NoCycles, NoNull);
        }

        public static bool operator ==(JsonSettings left, JsonSettings right) => left.Equals(right);
        public static bool operator !=(JsonSettings left, JsonSettings right) => !left.Equals(right);
    }

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
#if FANTASY_NET
            /// <summary>
            /// .NET自带, 微软提供的Json库, 性能占优势。
            /// </summary>
            Microsoft,
#endif
#if FANTASY_UNITY
            /// <summary>
            /// Unity提供的Json库, 用于支持各种Unity特供的类型序列化与反序列化。
            /// 注意: 其在 <see cref="JsonSettings"/> 的功能支持非常有限。一些无效设置将被忽视。
            /// </summary>
            UnityJson,
#endif
            /// <summary>
            /// 第三方开发者Newtonsoft提供的Json库, 泛用性较好。
            /// </summary>
            Newtonsoft,
        }

        /// <summary>
        /// 标识符
        /// </summary>
        internal class Mark
        {
            /// <summary>
            /// 代表 微软
            /// </summary>
            public const string M = "M";
            /// <summary>
            /// 代表 Unity
            /// </summary>
            public const string U = "U";
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
            public const string L = "$L";
            /// <summary>
            /// 数据存放处
            /// </summary>
            public const string D = "$D";
            /// <summary>
            /// 类型标识
            /// </summary>
            public const string T = "$T";
        }

        #region 把JsonSettings分别映射到提供方的设置项

        // ** 这里缓存采用 List 而不是 HashSet, 因为元素少的情况下遍历列表取值很快 **//
#if FANTASY_NET
        private static readonly List<(JsonSettings, JsonSerializerOptions)> _serializerSettingsCache_M = new();
#endif
        private static readonly List<(JsonSettings, JsonSerializerSettings)> _serializerSettingsCache_N = new();


        // ** 线程安全缓存 **//
#if FANTASY_NET
        private static readonly List<(JsonSettings, JsonSerializerOptions)> _lockedCache_M = new();
#endif
        private static readonly List<(JsonSettings, JsonSerializerSettings)> _lockedCache_N = new();
        private static readonly object _lock_M = new object();
        private static readonly object _lock_N = new object();

        // Newtonsoft序列化器默认设置
        private readonly static JsonSerializerSettings newtonsoftDefaultSettings = new()
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };

#if FANTASY_NET
        // Microsoft序列化器默认设置 
        private readonly static JsonSerializerOptions microsoftDefaultOptions = new()
        {
#if NET9_0_OR_GREATER
            AllowOutOfOrderMetadataProperties = true,
#endif
            TypeInfoResolver = ResolverWithPolymorphism, //开启多态鉴别
            ReferenceHandler = ReferenceHandler.Preserve,
        };

        /// <summary>
        /// 开启多态鉴别
        /// </summary>
        public static readonly IJsonTypeInfoResolver ResolverWithPolymorphism = JsonTypeInfoResolver.Combine(
                            new EntityPolymorphismResolver()
                        );
        /// <summary>
        /// 关闭多态鉴别
        /// </summary>
        public static readonly IJsonTypeInfoResolver ResolverWithoutPolymorphism = JsonTypeInfoResolver.Combine(
                       new DisablePolymorphismResolver()
                   ); //关闭多态鉴别

        private static JsonSerializerOptions MakeMicrosoftOptions(JsonSettings settings, bool threadSafe = false)
        {

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
#if NET9_0_OR_GREATER
                AllowOutOfOrderMetadataProperties = true,
#endif
                WriteIndented = settings.IsIndented,
                ReferenceHandler = settings.NoCycles ? ReferenceHandler.IgnoreCycles : ReferenceHandler.Preserve,
                TypeInfoResolver = settings.WriteTypeWhenNecessary ? ResolverWithPolymorphism : ResolverWithoutPolymorphism,
                DefaultIgnoreCondition = settings.NoNull ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never,
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
#endif
        private static JsonSerializerSettings MakeNewtonsoftSettings(JsonSettings settings, bool threadSafe = false)
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
                ReferenceLoopHandling = settings.NoCycles ? ReferenceLoopHandling.Ignore : ReferenceLoopHandling.Serialize,
                TypeNameHandling = settings.WriteTypeWhenNecessary ? TypeNameHandling.Auto : TypeNameHandling.None,
                Formatting = settings.IsIndented ? Formatting.Indented : Formatting.None,
                NullValueHandling = settings.NoNull ? NullValueHandling.Ignore : NullValueHandling.Include,
            };

            // 添加到缓存
            if (!threadSafe)
            {
                _serializerSettingsCache_N.Add((settings, setting));
            }
            else lock (_lock_N)
            {
                _lockedCache_N.Add((settings, setting));
            }

            return setting;
        }

        #endregion

        /// <summary>
        /// 将对象序列化为 JSON 字符串。允许传入序列化器的相关设置。
        /// </summary>
        /// <typeparam name="T">要序列化的对象类型。</typeparam>
        /// <param name="t">要序列化的对象。</param>
        /// <param name="settings">序列化器设置</param>
        /// <param name="isCacheThreadSafe">将缓存设置为线程安全, 默认为 false ;如果开启线程安全, 自动加锁会导致性能略微降低. </param>
        public static string ToJson<T>(this T t, JsonSettings? settings = null, bool isCacheThreadSafe = false)
        {
            // 默认直接用Newton的库
            if (settings == null)
                return JsonConvert.SerializeObject(t, newtonsoftDefaultSettings);

            // 创建包装器
            var wrapper = new JsonWrapper<T>
            {
                L = default,
                Data = t
            };

            string json = string.Empty;
            var lib = settings.Value.Library;
            wrapper.L = lib switch
            {
#if FANTASY_NET
                Library.Microsoft => Mark.M,
#endif
#if FANTASY_UNITY
                Library.UnityJson => Mark.U,
#endif
                Library.Newtonsoft => Mark.N,
                _ => throw new ArgumentOutOfRangeException(nameof(lib))
            };

            switch (lib)
            {
#if FANTASY_NET
                case Library.Microsoft:
                    json = MicrosoftJsonSerializer.Serialize(wrapper, MakeMicrosoftOptions(settings.Value, isCacheThreadSafe));
                    break;
#endif
#if FANTASY_UNITY
                case Library.UnityJson:
                    json = JsonUtility.ToJson(wrapper, settings.Value.IsIndented); break;
#endif
                case Library.Newtonsoft:
                    json = JsonConvert.SerializeObject(wrapper, MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe));
                    break;

                default:
                    throw new Exception("Unexpected: librarySelection is Unknown.");
            }

            return json;
        }

#if FANTASY_NET
        /// <summary>
        /// 高性能Json的序列化, 将对象序列化为 JSON Utf8 字节。
        /// <para>
        /// 仅由<see cref="MicrosoftJsonSerializer"/>提供支持。
        /// </para>
        /// </summary>
        /// <typeparam name="T">要序列化的对象类型。</typeparam>
        /// <param name="t">要序列化的对象。</param>
        /// <param name="opts">序列化器设置</param>
        public static ReadOnlySpan<byte> ToJsonBytes<T>(this T t, JsonSerializerOptions? opts = null)
        {
            if (t == null)
                return null;

            var bufferWriter = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(bufferWriter);
            MicrosoftJsonSerializer.Serialize(writer, t, microsoftDefaultOptions);
            writer.Flush();
            return bufferWriter.WrittenSpan;
        }
#endif

        private static readonly ConcurrentDictionary<Type, Type> _wrapperCache = new();

        /// <summary>
        /// 反序列化 JSON 字符串为指定类型的对象。
        /// </summary>
        /// <param name="json">要反序列化的 JSON 字符串。</param>
        /// <param name="type">目标对象的类型。</param>    
        /// <param name="settings">序列化器设置</param>
        /// <param name="detectMode">指定为<see cref="DetectMode.MustBeNormal"/>
        /// 或者<see cref="DetectMode.MustBeWrapper"/> 跳过自动检测, 性能更好；
        /// 设置为<see cref="DetectMode.Auto"/>如果开启, 会自动检测是否Wrapped、自动检测是哪个库, 代价是性能较差。 </param>
        /// <param name="isCacheThreadSafe">将缓存设置为线程安全, 默认为 false ;如果开启线程安全, 自动加锁会导致性能略微降低. </param>
        /// <returns>反序列化后的对象。</returns>
        public static object Deserialize(this string json, Type type, JsonSettings? settings = null, DetectMode detectMode = DetectMode.MustBeWrapper, bool isCacheThreadSafe = false)
        {
            bool isWrapped = default;

#if FANTASY_NET
            JsonElement LibMark_Element = default;
            JsonElement Data_Element = default;
#endif
#if FANTASY_UNITY
            JToken libMarkElement = default;
            JToken dataElement = default;
#endif
            if (detectMode == DetectMode.Auto)
            {
                // 自动探测是不是 Wrapper 结构
#if FANTASY_NET
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;

                isWrapped = root.ValueKind == JsonValueKind.Object &&
                            root.TryGetProperty(MetaPropertyStr.L, out LibMark_Element) &&
                            root.TryGetProperty(MetaPropertyStr.D, out Data_Element);
#endif
#if FANTASY_UNITY
                var root = JObject.Parse(json);
                isWrapped =
                    root.Type == JTokenType.Object &&
                    root.TryGetValue(MetaPropertyStr.L, out libMarkElement) &&
                    root.TryGetValue(MetaPropertyStr.D, out dataElement);
#endif
            }
            else if (detectMode == DetectMode.MustBeWrapper)
            {
                isWrapped = true;
            }
            else if (detectMode == DetectMode.MustBeNormal)
            {
                isWrapped = false;
            }

            if (isWrapped)
            {
                string? libraryMark = default;
                if (detectMode == DetectMode.Auto)
#if FANTASY_NET
                    libraryMark = LibMark_Element.GetString();
#endif
#if FANTASY_UNITY
                    libraryMark = libMarkElement.Value<string>();
#endif
                else if (settings != null)
                    libraryMark = settings.Value.GetLibraryMark();
                else
                    libraryMark = Mark.M;


                // 获取或构造闭合泛型 Wrapper<T> 类型
                Type wrapperType = _wrapperCache.GetOrAdd(type, typeof(JsonWrapper<>).MakeGenericType(type));

                switch (libraryMark)
                {
                    case Mark.M:  //使用微软库
                        {
#if FANTASY_NET
                            settings ??= new JsonSettings();
                            JsonSerializerOptions options = MakeMicrosoftOptions(settings.Value, isCacheThreadSafe);
                            object? wrapper = MicrosoftJsonSerializer.Deserialize(json, wrapperType, options);
                            return wrapper is IDataAccessible w ? w.AccessData() : null;

#endif
#if FANTASY_UNITY
                            throw new("Fantasy.Unity can not deserialize a JSON serialized by Microsoft`s System.Text.Json, you shall use Fantasy.Net or check your json file`s library selection.");
#endif
                        }
                    case Mark.U: //使用Unity库
                        {
#if FANTASY_NET
                            throw new("Fantasy.Net can not deserialize a JSON serialized by Unity`s JsonUtility, you shall use Fantasy.Unity or check your json file`s library selection.");
#endif
#if FANTASY_UNITY
                            if (settings != null)
                                Log.Info("You are trying to use advanced JsonSettings whitch may not be supported by Unity Json Utility.");
                            
                            var wrapper = JsonUtility.FromJson(json, wrapperType);
                            return wrapper is IDataAccessible w ? w.AccessData() : null;
#endif
                        }
                    case Mark.N:  //使用Newtonsoft库
                        {
                            settings ??= new JsonSettings();
                            JsonSerializerSettings options = MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe);
                            object? wrapper = JsonConvert.DeserializeObject(json, wrapperType, options);
                            return wrapper is IDataAccessible w ? w.AccessData() : null;
                        }
                    default: throw new Exception("Unexpected: Detected unknown Json library mark. Deserialize failed. ");
                }
            }
            else  // --- 处理未经包装的JSON  ---
            {
                settings ??= new JsonSettings();
                switch (settings.Value.Library)
                {
                    case Library.Newtonsoft:
                        return JsonConvert.DeserializeObject(json, type, MakeNewtonsoftSettings(settings.Value, isCacheThreadSafe));
#if FANTASY_NET
                    case Library.Microsoft:
                        return MicrosoftJsonSerializer.Deserialize(json, type, MakeMicrosoftOptions(settings.Value, isCacheThreadSafe));
#endif
#if FANTASY_UNITY
                    case Library.UnityJson:
                        return JsonUtility.FromJson(json, type);
#endif
                    default:
                        throw
                        new Exception($"Deserialize UnWrapped Type Failed for {type.Name}");
                }
            }
        }

        /// <summary>
        /// 反序列化 JSON 字符串为指定类型的对象。
        /// </summary>
        /// <typeparam name="T">目标对象的类型。</typeparam>
        /// <param name="json">要反序列化的 JSON 字符串。</param>
        /// <param name="settings">序列化器设置</param>
        /// <param name="detectMode">指定为<see cref="DetectMode.MustBeNormal"/>
        /// 或者<see cref="DetectMode.MustBeWrapper"/> 跳过自动检测, 性能更好；
        /// 设置为<see cref="DetectMode.Auto"/>如果开启, 会自动检测是否Wrapped、自动检测是哪个库, 代价是性能较差。 </param>
        /// <param name="isCacheThreadSafe">将缓存设置为线程安全, 默认为 false ;如果开启线程安全, 自动加锁会导致性能略微降低. </param>
        /// <returns>反序列化后的对象。</returns>
        public static T Deserialize<T>(this string json, JsonSettings? settings = null, DetectMode detectMode = DetectMode.MustBeWrapper, bool isCacheThreadSafe = false)
        {
            return (T)Deserialize(json, typeof(T), settings, detectMode, isCacheThreadSafe);
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
#if FANTASY_NET
    /// <summary>
    /// 打开实体多态鉴别配置器。这个类是针对采用微软的Json库的情况下序列化和反序列化的类型写入拓展，
    /// 用于将一个"<see langword="$T"/>"字段注入Json, 以记录基类的派生类的真实类型。
    /// </summary>
    internal class EntityPolymorphismResolver : DefaultJsonTypeInfoResolver
    {
        /// <summary>
        /// 覆写
        /// </summary>
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo info = base.GetTypeInfo(type, options);

            // 只针对框架里的基类 Entity 进行修改
            if (info.Type == typeof(Entity))
            {
                info.PolymorphismOptions ??= new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = MetaPropertyStr.T,
                    IgnoreUnrecognizedTypeDiscriminators = false, // 这个设置为false的效果: 不识别的鉴别器Id会抛出异常。
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization //如果遇到未知的子类将会 Fail
                };

                // 配置所有实体派生类
                foreach (var kv in AssemblyManifest.Manifests)
                {
                    var allEntityTypes = kv.Value.EntityTypeCollectionRegistrar.GetEntityTypes();
                    foreach (var one in allEntityTypes)
                    {
                        info.PolymorphismOptions.DerivedTypes.Add(
                           new JsonDerivedType(one, one.FullName!)
                       );
                    }
                }
            }

            return info;
        }
    }


    /// <summary>
    /// 关闭多态鉴别
    /// </summary>
    internal class DisablePolymorphismResolver : DefaultJsonTypeInfoResolver
    {
        /// <summary>
        /// 覆写
        /// </summary>
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

            if (jsonTypeInfo != null)
            {
                jsonTypeInfo.PolymorphismOptions = null; // 强制检测为非多态类型。
            }

            return jsonTypeInfo;
        }
    }

    ///// <summary>
    ///// 实体Json序列化上下文。
    ///// <para>
    ///// 注: 这个类不用写任何逻辑, 因为STJ的源码生成器会自动生成 ...
    ///// </para>
    ///// <para>
    ///// Note : 目前的所有派生实体是通过<see cref="DefaultJsonTypeInfoResolver.GetTypeInfo"/>方法动态注册的。
    ///// 由于STJ的源生成器无法识别自行实现的 IIncrementalGenerator , 所以
    ///// 理论上要达到最高性能的Json序列化, 有可能需要模仿STJ的官方源生成模板, 来自行实现一套STJ源生成。
    ///// 这个工作量浩大, 在AOT场景下的确具备高收益, 但是目前没有时间来做, 只能以后再考虑了.
    ///// 然而, 经过测试, 即便没有STJ源码优化, .NET的Json库也比Newtonsoft的要快。
    ///// </para>
    ///// </summary>
    //[JsonSerializable(typeof(Entity))]
    //public partial class EntityJsonContext : JsonSerializerContext
    //{
    //    // 注: 这个类不用写任何逻辑, 因为STJ的源码生成器会自动生成 ...
    //}
#endif
}