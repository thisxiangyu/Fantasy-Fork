using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Fantasy.Entitas.Interface;
using Fantasy.Entitas.TypeMeta;
using Fantasy.IdFactory;
using Fantasy.Pool;
using Fantasy.Database.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using ProtoBuf;
using MemoryPack;
using NJ = Newtonsoft.Json;
#if FANTASY_NET
using MJ = System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
#endif
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable MergeIntoPattern
// ReSharper disable SuspiciousTypeConversion.Global
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
// ReSharper disable CheckNamespace
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Entitas
{
    /// <summary>
    /// Entity接口
    /// </summary>
    public interface IEntity : IDisposable, IPool { }

    /// <summary>
    /// Entity的抽象类，各类业务Entity皆继承于此。
    /// </summary>
    public abstract partial class Entity : IEntity
    {
        #region Members

        /// <summary>
        /// 实体的Id
        /// </summary>
        [BsonId]
        [BsonElement]
        [BsonIgnoreIfDefault]
        [BsonDefaultValue(0L)]
        public long Id { get; protected set; }
        /// <summary>
        /// 实体的RunTimeId
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public long RuntimeId { get; protected set; }
        /// <summary>
        /// 当前实体是否已经被销毁
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public bool IsDisposed => RuntimeId == 0;
        /// <summary>
        /// 当前实体所归属的Scene
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public Scene Scene { get; protected set; }
        /// <summary>
        /// 实体的父实体
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public Entity Parent { get; protected set; }
        /// <summary>
        /// 实体的真实Type
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public Type Type { get; protected set; }
        /// <summary>
        /// 实体的真实Type的编码
        /// </summary>
        [BsonIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        public long TypeHashCode { get; private set; }
#if FANTASY_NET
        [BsonElement("_s")][BsonIgnoreIfNull][MJ.JsonInclude][MJ.JsonPropertyName("_s")][NJ.JsonProperty("_s")]
        internal EntityList<Entity> _singleDb;

        [BsonElement("_m")][BsonIgnoreIfNull][MJ.JsonInclude][MJ.JsonPropertyName("_m")][NJ.JsonProperty("_m")]
        internal EntityList<Entity> _multiDb;

        #region 判断是否为嵌入式DbSet, 目前有基于接口和基于Attri两种判断方式,未来可能只保留一种

        private bool? _isEmbeddedCache;
        //基于接口判断
        internal bool IsEmbeddedIDbSet()
        {
            if (_isEmbeddedCache == null)
                _isEmbeddedCache = (this is IDbSet dbSet) && dbSet.DbSetOpts != null && dbSet.DbSetOpts.IsEmbedded;

            return _isEmbeddedCache.Value;
        }
        //基于DbSetAttri判断
        internal bool IsAnnotatedAsEmbedded()
        {
            if (_isEmbeddedCache == null)
            {
                long code = TypeHashCache.GetHashCode(this.Type);
                _isEmbeddedCache = TypeDbSetChecker.InfoByHash == null
                                   ? false
                                   : TypeDbSetChecker.InfoByHash[code].IsEmbedded();
            }

            return _isEmbeddedCache.Value;
        }

        #endregion
#endif        

        [BsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        private EntitySortedDictionary<long, Entity> _single;

        [BsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        private EntitySortedDictionary<long, Entity> _multi;

        /// <summary>
        /// 获得父Entity
        /// </summary>
        /// <typeparam name="T">父实体的泛型类型</typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetParent<T>() where T : Entity, new()
        {
            return Parent as T;
        }

        /// <summary>
        /// 获取当前实体的网络地址。
        /// </summary>
        [BsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        public long Address => RuntimeId;

        #endregion

        #region Create

        /// <summary>
        /// 创建一个实体,默认在对象池创建,执行组件事件。
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="type">实体的Type</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Create(Scene scene, Type type)
        {
            return Create(scene, type, scene.EntityIdFactory.Create, true, true);
        }

        /// <summary>
        /// 创建一个实体,默认执行组件事件。
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="type">实体的Type</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Create(Scene scene, Type type, bool isPool)
        {
            return Create(scene, type, scene.EntityIdFactory.Create, isPool, true);
        }

        /// <summary>
        /// 创建一个实体
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="type">实体的Type</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <param name="isRunEvent">是否执行实体事件</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Create(Scene scene, Type type, bool isPool, bool isRunEvent)
        {
            return Create(scene, type, scene.EntityIdFactory.Create, isPool, isRunEvent);
        }

        /// <summary>
        /// 创建一个实体
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="type">实体的Type</param>
        /// <param name="id">指定实体的Id</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <param name="isRunEvent">是否执行实体事件</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Create(Scene scene, Type type, long id, bool isPool, bool isRunEvent)
        {
            if (!typeof(Entity).IsAssignableFrom(type))
            {
                throw new NotSupportedException($"Type:{type.FullName} must inherit from Child");
            }

            Entity entity = null;

            if (isPool)
            {
                entity = (Entity)scene.EntityPool.Rent(scene, type);
            }
            else
            {
                entity = scene.PoolGeneratorComponent.Create<Entity>(type);
            }

            entity.Scene = scene;
            entity.Type = type;
            entity.TypeHashCode = TypeHashCache.GetHashCode(type);
            entity.SetIsPool(isPool);
            entity.Id = id;
            entity.RuntimeId = scene.RuntimeIdFactory.Create(isPool);
            scene.AddEntity(entity);

            if (isRunEvent)
            {
                scene.EntityComponent.Awake(entity);
                scene.EntityComponent.RegisterUpdate(entity);
#if FANTASY_UNITY
                scene.EntityComponent.RegisterLateUpdate(entity);
#endif
            }

            return entity;
        }

        /// <summary>
        /// 创建一个实体,默认在对象池创建,执行组件事件。
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <typeparam name="T">要创建的实体泛型类型</typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Create<T>(Scene scene) where T : Entity, new()
        {
            return Create<T>(scene, scene.EntityIdFactory.Create, true, true);
        }

        /// <summary>
        /// 创建一个实体,默认执行组件事件。
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <typeparam name="T">要创建的实体泛型类型</typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Create<T>(Scene scene, bool isPool) where T : Entity, new()
        {
            return Create<T>(scene, scene.EntityIdFactory.Create, isPool, true);
        }

        /// <summary>
        /// 创建一个实体
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <param name="isRunEvent">是否执行实体事件</param>
        /// <typeparam name="T">要创建的实体泛型类型</typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Create<T>(Scene scene, bool isPool, bool isRunEvent) where T : Entity, new()
        {
            return Create<T>(scene, scene.EntityIdFactory.Create, isPool, isRunEvent);
        }

        /// <summary>
        /// 创建一个实体
        /// </summary>
        /// <param name="scene">所属的Scene</param>
        /// <param name="id">指定实体的Id</param>
        /// <param name="isPool">是否从对象池创建，如果选择的是，销毁的时候同样会进入对象池</param>
        /// <param name="isRunEvent">是否执行实体事件</param>
        /// <typeparam name="T">要创建的实体泛型类型</typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Create<T>(Scene scene, long id, bool isPool, bool isRunEvent) where T : Entity, new()
        {
            var entity = isPool ? scene.EntityPool.Rent<T>() : new T();
            entity.Scene = scene;
            entity.Type = typeof(T);
            entity.TypeHashCode = TypeHashCache<T>.HashCode;
            entity.SetIsPool(isPool);
            entity.Id = id;
            entity.RuntimeId = scene.RuntimeIdFactory.Create(isPool);
            scene.AddEntity(entity);

            if (isRunEvent)
            {
                scene.EntityComponent.Awake(entity);
                scene.EntityComponent.RegisterUpdate(entity);
#if FANTASY_UNITY
                scene.EntityComponent.RegisterLateUpdate(entity);
#endif
            }

            return entity;
        }

        #endregion

        #region Append

        /// <summary>
        /// 添加一个子实体到当前实体上
        /// </summary>
        /// <param name="isPool">是否从对象池里创建</param>
        /// <typeparam name="T">要添加子实体的泛型类型</typeparam>
        /// <returns>返回添加到实体上子实体的实例</returns>
        public T AddComponent<T>(bool isPool = true) where T : Entity, new()
        {
            var entity = Create<T>(Scene, Scene.EntityIdFactory.Create, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.RegisterUpdate(entity);
#if FANTASY_UNITY
            Scene.EntityComponent.RegisterLateUpdate(entity);
#endif
            return entity;
        }

        /// <summary>
        /// 添加一个子实体到当前实体上
        /// </summary>
        /// <param name="id">要添加子实体的Id</param>
        /// <param name="isPool">是否从对象池里创建</param>
        /// <typeparam name="T">要添加子实体的泛型类型</typeparam>
        /// <returns>返回添加到实体上子实体的实例</returns>
        public T AddComponent<T>(long id, bool isPool = true) where T : Entity, new()
        {
            var entity = Create<T>(Scene, id, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.RegisterUpdate(entity);
#if FANTASY_UNITY
            Scene.EntityComponent.RegisterLateUpdate(entity);
#endif
            return entity;
        }

        /// <summary>
        /// 添加一个子实体到当前实体上
        /// </summary>
        /// <param name="subEntity">要添加的实体实例</param>
        public void AddComponent(Entity subEntity)
        {
            if (this == subEntity)
            {
                Log.Error("Cannot add oneself to one's own Sub-Child");
                return;
            }

            if (subEntity.IsDisposed)
            {
                Log.Error($"Sub-Child is Disposed {subEntity.Type.FullName}");
                return;
            }

            var type = subEntity.Type;
            subEntity.Parent?.RemoveComponent(subEntity, false);

            if (subEntity is IMultiAppended)
            {
                _multi ??= Scene.EntitySortedDictionaryPool.Rent();

                try
                {
                    _multi.Add(subEntity.Id, subEntity);
                }
                catch (Exception ex)
                {
                    throw new Exception($"A same id {subEntity.Id} of {subEntity.Type} is existing in {GetType()} : {ex}");
                }

#if FANTASY_NET
                if (subEntity.IsAnnotatedAsEmbedded())
                {
                    _multiDb ??= Scene.EntityListPool.Rent();
                    _multiDb.Add(subEntity);
                }
#endif
            }
            else
            {
                var typeHashCode = subEntity.TypeHashCode;

                if (_single == null)
                {
                    _single = Scene.EntitySortedDictionaryPool.Rent();
                }
                else if (_single.ContainsKey(typeHashCode))
                {
                    Log.Error($"type:{type.FullName} If you want to append multiple entites of the same type, please implement IMultiAppended");
                    return;
                }

                _single.Add(typeHashCode, subEntity);
#if FANTASY_NET
                if (subEntity.IsAnnotatedAsEmbedded())
                {
                    _singleDb ??= Scene.EntityListPool.Rent();
                    _singleDb.Add(subEntity);
                }
#endif
            }

            subEntity.Parent = this;
            subEntity.Scene = Scene;
        }

        /// <summary>
        /// 添加一个子实体到当前实体上
        /// </summary>
        /// <param name="subEntity">要添加的实体实例</param>
        /// <typeparam name="T">要添加子实体的泛型类型</typeparam>
        public void AddComponent<T>(T subEntity) where T : Entity
        {
            if (this == subEntity)
            {
                Log.Error("Cannot add oneself to one's own subEntitys");
                return;
            }

            if (subEntity.IsDisposed)
            {
                Log.Error($"SubEntity is Disposed {typeof(T).FullName}");
                return;
            }

            subEntity.Parent?.RemoveComponent(subEntity, false);

            if (TypeSupportedChecker<T>.IsMulti)
            {
                _multi ??= Scene.EntitySortedDictionaryPool.Rent();
                try
                {
                    _multi.Add(subEntity.Id, subEntity);
                }
                catch (Exception ex)
                {
                    throw new Exception($"A same id {subEntity.Id} of {subEntity.Type} is existing in {GetType()} : {ex}");
                }
#if FANTASY_NET
                if (TypeDbSetChecker<T>.IsEmbedded)
                {
                    _multiDb ??= Scene.EntityListPool.Rent();
                    _multiDb.Add(subEntity);
                }
#endif
            }
            else
            {
                var typeHashCode = subEntity.TypeHashCode;

                if (_single == null)
                {
                    _single = Scene.EntitySortedDictionaryPool.Rent();
                }
                else if (_single.ContainsKey(typeHashCode))
                {
                    Log.Error($"type:{typeof(T).FullName} If you want to append multiple entites of the same type, please implement IMultiAppended");
                    return;
                }

                _single.Add(typeHashCode, subEntity);
#if FANTASY_NET
                if (TypeDbSetChecker<T>.IsEmbedded)
                {
                    _singleDb ??= Scene.EntityListPool.Rent();
                    _singleDb.Add(subEntity);
                } 
#endif
            }

            subEntity.Parent = this;
            subEntity.Scene = Scene;
        }

        /// <summary>
        /// 添加一个子实体到当前实体上
        /// </summary>
        /// <param name="type">子实体的类型</param>
        /// <param name="isPool">是否在对象池创建</param>
        /// <returns></returns>
        public Entity AddComponent(Type type, bool isPool = true)
        {
            var id = typeof(IMultiAppended).IsAssignableFrom(type) ? Scene.EntityIdFactory.Create : Id;
            var entity = Entity.Create(Scene, type, id, isPool, false);
            AddComponent(entity);
            Scene.EntityComponent.Awake(entity);
            Scene.EntityComponent.RegisterUpdate(entity);
#if FANTASY_UNITY
            Scene.EntityComponent.RegisterLateUpdate(entity);
#endif
            return entity;
        }

        #endregion

        #region HasSubEntity

        /// <summary>
        /// 当前实体上是否有指定类型的子实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>() where T : Entity, new()
        {
            if (_single == null)
            {
                return false;
            }

            return _single.ContainsKey(TypeHashCache<T>.HashCode);
        }

        /// <summary>
        /// 当前实体上是否有指定类型的子实体
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(Type type)
        {
            if (_single == null)
            {
                return false;
            }

            return _single.ContainsKey(TypeHashCache.GetHashCode(type));
        }

        /// <summary>
        /// 当前实体上是否有指定类型的子实体
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(long id) where T : Entity, IMultiAppended, new()
        {
            if (_multi == null)
            {
                return false;
            }

            return _multi.ContainsKey(id);
        }

        #endregion

        #region GetSubEntity

        /// <summary>
        /// 当前实体上查找一个子实体
        /// </summary>
        /// <typeparam name="T">要查找实体泛型类型</typeparam>
        /// <returns>查找的实体实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>() where T : Entity, new()
        {
            if (_single == null)
            {
                return null;
            }

            return _single.TryGetValue(TypeHashCache<T>.HashCode, out var subEntity) ? (T)subEntity : null;
        }

        /// <summary>
        /// 当前实体上查找一个子实体
        /// </summary>
        /// <param name="type">要查找实体类型</param>
        /// <returns>查找的实体实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetComponent(Type type)
        {
            if (_single == null)
            {
                return null;
            }

            return _single.GetValueOrDefault(TypeHashCache.GetHashCode(type));
        }

        /// <summary>
        /// 当前实体上查找一个子实体
        /// </summary>
        /// <param name="id">要查找实体的Id</param>
        /// <typeparam name="T">要查找实体泛型类型</typeparam>
        /// <returns>查找的实体实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(long id) where T : Entity, IMultiAppended, new()
        {
            if (_multi == null)
            {
                return null;
            }

            return _multi.TryGetValue(id, out var entity) ? (T)entity : null;
        }

        /// <summary>
        /// 当前实体上查找一个子实体，如果没有就创建一个新的并添加到当前实体上
        /// </summary>
        /// <param name="isPool">是否从对象池创建</param>
        /// <typeparam name="T">要查找或添加实体泛型类型</typeparam>
        /// <returns>查找的实体实例</returns>
        public T GetOrAddComponent<T>(bool isPool = true) where T : Entity, new()
        {
            return GetComponent<T>() ?? AddComponent<T>(isPool);
        }

        #endregion

        #region RemoveSubEntity

        /// <summary>
        /// 当前实体下删除一个子实体
        /// </summary>
        /// <param name="isDispose">是否执行删除实体的Dispose方法</param>
        /// <typeparam name="T">实体的泛型类型</typeparam>
        /// <exception cref="NotSupportedException"></exception>
        public void RemoveComponent<T>(bool isDispose = true) where T : Entity, new()
        {
            if (TypeSupportedChecker<T>.IsMulti)
            {
                throw new NotSupportedException($"{typeof(T).FullName} message:Cannot delete entity that implement the IMultiAppended interface");
            }

            if (_single == null)
            {
                return;
            }

            var typeHashCode = TypeHashCache<T>.HashCode;
            if (!_single.TryGetValue(typeHashCode, out var subEntity))
            {
                return;
            }
#if FANTASY_NET
            if (_singleDb != null && TypeDbSetChecker<T>.IsEmbedded)
            {
                _singleDb.Remove(subEntity);

                if (_singleDb.Count == 0)
                {
                    Scene.EntityListPool.Return(_singleDb);
                    _singleDb = null;
                }
            }
#endif
            _single.Remove(typeHashCode);

            if (_single.Count == 0)
            {
                Scene.EntitySortedDictionaryPool.Return(_single);
                _single = null;
            }

            if (isDispose)
            {
                subEntity.Dispose();
            }
        }

        /// <summary>
        /// 当前实体下删除一个实体
        /// </summary>
        /// <param name="id">要删除的实体Id</param>
        /// <param name="isDispose">是否执行删除实体的Dispose方法</param>
        /// <typeparam name="T">实体的泛型类型</typeparam>
        public void RemoveComponent<T>(long id, bool isDispose = true) where T : Entity, IMultiAppended, new()
        {
            if (_multi == null)
            {
                return;
            }

            if (!_multi.TryGetValue(id, out var subEntity))
            {
                return;
            }
#if FANTASY_NET
            if (_multiDb != null && TypeDbSetChecker<T>.IsEmbedded)
            {
                _multiDb.Remove(subEntity);
                if (_multiDb.Count == 0)
                {
                    Scene.EntityListPool.Return(_multiDb);
                    _multiDb = null;
                }
            }
#endif
            _multi.Remove(subEntity.Id);
            if (_multi.Count == 0)
            {
                Scene.EntitySortedDictionaryPool.Return(_multi);
                _multi = null;
            }

            if (isDispose)
            {
                subEntity.Dispose();
            }
        }

        /// <summary>
        /// 当前实体下删除一个实体
        /// </summary>
        /// <param name="subEntity">要删除的实体实例</param>
        /// <param name="isDispose">是否执行删除实体的Dispose方法</param>
        public void RemoveComponent(Entity subEntity, bool isDispose = true)
        {
            if (this == subEntity)
            {
                return;
            }

            if (subEntity is IMultiAppended)
            {
                if (_multi != null)
                {
                    if (!_multi.ContainsKey(subEntity.Id))
                    {
                        return;
                    }
#if FANTASY_NET
                    if (subEntity.IsAnnotatedAsEmbedded())
                    {
                        _multiDb.Remove(subEntity);
                        if (_multiDb.Count == 0)
                        {
                            Scene.EntityListPool.Return(_multiDb);
                            _multiDb = null;
                        }
                    }
#endif
                    _multi.Remove(subEntity.Id);
                    if (_multi.Count == 0)
                    {
                        Scene.EntitySortedDictionaryPool.Return(_multi);
                        _multi = null;
                    }
                }
            }
            else if (_single != null)
            {
                var typeHashCode = subEntity.TypeHashCode;
                if (!_single.ContainsKey(typeHashCode))
                {
                    return;
                }
#if FANTASY_NET
                if (_singleDb != null && subEntity.IsAnnotatedAsEmbedded())
                {
                    _singleDb.Remove(subEntity);

                    if (_singleDb.Count == 0)
                    {
                        Scene.EntityListPool.Return(_singleDb);
                        _singleDb = null;
                    }
                }
#endif
                _single.Remove(typeHashCode);

                if (_single.Count == 0)
                {
                    Scene.EntitySortedDictionaryPool.Return(_single);
                    _single = null;
                }
            }

            if (isDispose)
            {
                subEntity.Dispose();
            }
        }

        /// <summary>
        /// 当前实体下删除一个实体
        /// </summary>
        /// <param name="subEntity">要删除的实体实例</param>
        /// <param name="isDispose">是否执行删除实体的Dispose方法</param>
        /// <typeparam name="T">实体的泛型类型</typeparam>
        public void RemoveComponent<T>(T subEntity, bool isDispose = true) where T : Entity
        {
            if (this == subEntity)
            {
                return;
            }

            if (TypeSupportedChecker<T>.IsMulti)
            {
                if (_multi != null)
                {
                    if (!_multi.ContainsKey(subEntity.Id))
                    {
                        return;
                    }
#if FANTASY_NET
                    if (TypeDbSetChecker<T>.IsEmbedded)
                    {
                        _multiDb.Remove(subEntity);
                        if (_multiDb.Count == 0)
                        {
                            Scene.EntityListPool.Return(_multiDb);
                            _multiDb = null;
                        }
                    }
#endif
                    _multi.Remove(subEntity.Id);
                    if (_multi.Count == 0)
                    {
                        Scene.EntitySortedDictionaryPool.Return(_multi);
                        _multi = null;
                    }
                }
            }
            else if (_single != null)
            {
                var typeHashCode = TypeHashCache<T>.HashCode;
                if (!_single.ContainsKey(typeHashCode))
                {
                    return;
                }
#if FANTASY_NET
                if (_singleDb != null && TypeDbSetChecker<T>.IsEmbedded)
                {
                    _singleDb.Remove(subEntity);

                    if (_singleDb.Count == 0)
                    {
                        Scene.EntityListPool.Return(_singleDb);
                        _singleDb = null;
                    }
                }
#endif
                _single.Remove(typeHashCode);

                if (_single.Count == 0)
                {
                    Scene.EntitySortedDictionaryPool.Return(_single);
                    _single = null;
                }
            }

            if (isDispose)
            {
                subEntity.Dispose();
            }
        }

        #endregion

        #region Deserialize

        /// <summary>
        /// 反序列化当前实体，因为在数据库加载过来的或通过协议传送过来的实体并没有跟当前Scene做关联。
        /// 所以必须要执行一下这个反序列化的方法才可以使用。
        /// </summary>
        /// <param name="scene">Scene</param>
        /// <param name="resetId">是否是重新生成实体的Id,如果是数据库加载过来的一般是不需要的</param>
        public void Deserialize(Scene scene, bool resetId = false)
        {
            if (RuntimeId != 0)
            {
                return;
            }

            try
            {
                Scene = scene;
                Type ??= GetType();
                TypeHashCode = TypeHashCache.GetHashCode(Type);
                RuntimeId = Scene.RuntimeIdFactory.Create(false);
                if (resetId)
                {
                    Id = RuntimeId;
                }
#if FANTASY_NET
                if (_singleDb != null && _singleDb.Count > 0)
                {
                    _single = Scene.EntitySortedDictionaryPool.Rent();
                    foreach (var entity in _singleDb)
                    {
                        entity.Parent = this;
                        entity.Type = entity.GetType();
                        _single.Add(TypeHashCache.GetHashCode(entity.Type), entity);
                        entity.Deserialize(scene, resetId);
                    }
                }

                if (_multiDb != null && _multiDb.Count > 0)
                {
                    _multi = Scene.EntitySortedDictionaryPool.Rent();
                    foreach (var entity in _multiDb)
                    {
                        entity.Parent = this;
                        entity.Deserialize(scene, resetId);
                        _multi.Add(entity.Id, entity);
                    }
                }
#endif
                scene.AddEntity(this);
                scene.EntityComponent.Deserialize(this);
            }
            catch (Exception e)
            {
                if (RuntimeId != 0)
                {
                    scene.RemoveEntity(RuntimeId);
                }

                Log.Error(e);
            }
        }

        #endregion

        #region ForEach

        /// <summary>
        /// 查询当前实体下的实现了IMultiAppended接口的实体
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public IEnumerable<Entity> ForEachAllMulti
        {
            get
            {
                if (_multi == null)
                {
                    yield break;
                }

                foreach (var (_, supportedMultiEntity) in _multi)
                {
                    yield return supportedMultiEntity;
                }
            }
        }
        /// <summary>
        /// 查找当前实体下的所有某个类型的MultiAppended的子实体
        /// </summary>
        public IEnumerable<T> ForEachMulti<T>() where T : Entity, IMultiAppended
        {
            if (_multi == null)
            {
                yield break;
            }

            foreach (var (_, entity) in _multi)
            {
                if (entity is T res)
                    yield return res;
            }
        }
        /// <summary>
        /// 查找当前实体下的所有子实体，不包括实现IMultiAppended接口的实体
        /// </summary>
        [BsonIgnore]
#if FANTASY_NET
        [NotMapped]
        [MJ.JsonIgnore]
#endif
        [NJ.JsonIgnore]
        [MemoryPackIgnore]
        [IgnoreDataMember]
        [ProtoIgnore]
        public IEnumerable<Entity> ForEachAllSingle
        {
            get
            {
                if (_single == null)
                {
                    yield break;
                }

                foreach (var (_, entity) in _single)
                {
                    yield return entity;
                }
            }
        }
        #endregion

        #region Dispose

        /// <summary>
        /// 销毁当前实体，销毁后会自动销毁当前实体下的所有实体。
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            var scene = Scene;
            var runTimeId = RuntimeId;
            RuntimeId = 0;

            if (_single != null)
            {
                foreach (var (_, entity) in _single)
                {
                    entity.Dispose();
                }

                _single.Clear();
                scene.EntitySortedDictionaryPool.Return(_single);
                _single = null;
            }

            if (_multi != null)
            {
                foreach (var (_, entity) in _multi)
                {
                    entity.Dispose();
                }

                _multi.Clear();
                scene.EntitySortedDictionaryPool.Return(_multi);
                _multi = null;
            }
#if FANTASY_NET
            if (_singleDb != null)
            {
                _singleDb.Clear();
                scene.EntityListPool.Return(_singleDb);
                _singleDb = null;
            }
            
            if (_multiDb != null)
            {
                _multiDb.Clear();
                scene.EntityListPool.Return(_multiDb);
                _multiDb = null;
            }
#endif
            scene.EntityComponent.Destroy(this);

            if (Parent != null && Parent != this && !Parent.IsDisposed)
            {
                Parent.RemoveComponent(this, false);
                Parent = null;
            }

            Id = 0;
            Scene = null;
            Parent = null;
            scene.RemoveEntity(runTimeId);
            scene.EntityPool.Return(Type, this);
            Type = null;
        }

        #endregion

        #region Pool

        /// <summary>
        /// 获取一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPool()
        {
            return IdFactoryHelper.RuntimeIdTool.GetIsPool(RuntimeId);
        }

        /// <summary>
        /// 设置一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <param name="isPool"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIsPool(bool isPool) { }

        #endregion
    }
}