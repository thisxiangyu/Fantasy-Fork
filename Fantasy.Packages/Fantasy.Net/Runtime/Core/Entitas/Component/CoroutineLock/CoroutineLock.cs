using System;
using Fantasy.Pool;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Fantasy.Async
{
    /// <summary>
    /// 协程锁专用的对象池
    /// </summary>
    public sealed class CoroutineLockPool : PoolStack<CoroutineLock>
    {
        /// <summary>
        /// 协程锁专用的对象池的构造函数
        /// </summary>
        public CoroutineLockPool() : base(2000) { }
    }

    /// <summary>
    /// 对锁的抽象
    /// </summary>
    public interface ILock
    {
        /// <summary>
        /// 锁职责
        /// </summary>
        public long LockDuty { get; }
        /// <summary>
        /// 释放掉
        /// </summary>
        void Release(long waitingKey);
    }

    /// <summary>
    /// 协程锁。对协程锁的基本抽象, 方便用于扩展协程锁。
    /// </summary>
    internal interface ICoroutineLock: ILock
    {
        public Scene Scene { get; }
        public CoroutineLockComponent CoroutineLockComponent { get; }
        FTask<WaitCoroutineLock> Wait(long waitForId, string tag = null, int timeOut = 30000);
    }

    /// <summary>
    /// 协程锁
    /// </summary>
    public sealed class CoroutineLock : ICoroutineLock, IPool, IDisposable
    {
        /// 所在Scene
        public Scene Scene { get; private set; }
        /// 池源
        public CoroutineLockComponent CoroutineLockComponent { get; private set; }
        private readonly Dictionary<long, CoroutineLockQueue> _queue = new Dictionary<long, CoroutineLockQueue>();
        /// <summary>
        /// 表示是否是对象池中创建的
        /// </summary>
        private bool _isPool;
        /// <summary>
        /// 协程锁职责
        /// </summary>
        public long LockDuty { get; private set; }

        internal void Initialize(CoroutineLockComponent coroutineLockComponent, ref long lockDuty)
        {
            Scene = coroutineLockComponent.Scene;
            LockDuty = lockDuty;
            CoroutineLockComponent = coroutineLockComponent;
        }
        /// <summary>
        /// 销毁协程锁，如果调用了该方法，所有使用当前协程锁等待的逻辑会按照顺序释放锁。
        /// </summary>
        public void Dispose()
        {
            foreach (var (_, coroutineLockQueue) in _queue)
            {
                while (TryCoroutineLockQueueDequeue(coroutineLockQueue)) { }
            }

            _queue.Clear();
            Scene = null;
            LockDuty = 0;
            CoroutineLockComponent = null;
        }
        /// <summary>
        /// 等待上一个任务完成
        /// </summary>
        /// <param name="waitForId">需要等待的Id</param>
        /// <param name="tag">用于查询协程锁的标记，可不传入，只有在超时的时候排查是哪个锁超时时使用</param>
        /// <param name="timeOut">等待多久会超时，当到达设定的时候会把当前锁给按照超时处理</param>
        /// <returns></returns>
        public async FTask<WaitCoroutineLock> Wait(long waitForId, string tag = null, int timeOut = 10000)
        {
            var waitCoroutineLock = CoroutineLockComponent.WaitCoroutineLockPool.Rent(this, ref waitForId, tag, timeOut);

            if (!_queue.TryGetValue(waitForId, out var queue))
            {
                queue = CoroutineLockComponent.CoroutineLockQueuePool.Rent();
                _queue.Add(waitForId, queue);
                return waitCoroutineLock;
            }

            queue.Enqueue(waitCoroutineLock);
            return await waitCoroutineLock.Tcs;
        }
        /// <summary>
        /// 按照先入先出的顺序，释放最早的一个协程锁
        /// </summary>
        /// <param name="waitingKey"></param>
        public void Release(long waitingKey)
        {
            if (!_queue.TryGetValue(waitingKey, out var coroutineLockQueue))
            {
                return;
            }

            if (!TryCoroutineLockQueueDequeue(coroutineLockQueue))
            {
                _queue.Remove(waitingKey);
            }
        }

        private bool TryCoroutineLockQueueDequeue(CoroutineLockQueue coroutineLockQueue)
        {
            if (!coroutineLockQueue.TryDequeue(out var waitCoroutineLock))
            {
                CoroutineLockComponent.CoroutineLockQueuePool.Return(coroutineLockQueue);
                return false;
            }

            if (waitCoroutineLock.TimerId != 0)
            {
                Scene.TimerComponent.Net.Remove(waitCoroutineLock.TimerId);
            }

            try
            {
                // 放到下一帧执行,如果不这样会导致逻辑的顺序不正常。
                Scene.ThreadSynchronizationContext.Post(waitCoroutineLock.SetResult);
            }
            catch (Exception e)
            {
                Log.Error($"Error in disposing CoroutineLock: {e}");
            }

            return true;
        }

        /// <summary>
        /// 获取一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <returns></returns>
        public bool IsPool()
        {
            return _isPool;
        }

        /// <summary>
        /// 设置一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <param name="isPool"></param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }
    }

    /// <summary>
    /// FTask 限流锁。 基于.NET 的信号量 (System.Threading.SemaphoreSlim) 实现。
    /// 【用途】 
    /// 将超过锁所设定上限数量值的并发FTask(任务)上锁, 等有空余任务位后再执行。
    /// 也可以像普通协程锁一样锁住某个Id, 且性能会稍佳。
    /// 
    /// 【典型使用场合】  
    /// 数据库操作层用其限制 FTask 数额以防止连接数据库请求并发量过大。
    /// 
    /// 【代价】
    /// 由于它采用Array 分配锁钥的Id, 数组不能Clear(), 这导致每次创建或 ResizeLimit时 会产生新的数组分配, 
    /// 非池化, 遵循引用清零自动GC的原则, 频繁创建是不可容忍的!
    /// 强烈建议使用时根据"职责"规划，CleanAsync 之后调用 Reset 从而复用， 而不是销毁掉。
    /// 此外, 在手动销毁、 调用 AsyncDispose 时 ,请注意其 AsyncDispose的异步性。
    /// 
    /// </summary>
    public sealed class FTaskFlowLock : ICoroutineLock
    {
        /// <summary>
        /// FTask 限制量
        /// </summary>
        public int FTaskFlowLimit { get; private set; }

        /// <summary>
        /// 取当前临界区中的 FTask 数量
        /// </summary>
        public int ActiveCount() { return _activeCount; }

        /// <summary>
        /// 当前等待中的任务数量（排队中）
        /// </summary>
        public int WaitingCount() { return _waitingCount; }

        private int _activeCount = 0;
        private int _waitingCount = 0;

        private SemaphoreSlim[] _idSem;// 按 ID 串行
        private long loopingNumber = 0; // 一个自循环的数字, 用来限流

        /// 所在Scene
        public Scene Scene { get; private set; }
        /// 池源
        public CoroutineLockComponent CoroutineLockComponent { get; private set; }
        /// 锁职责
        public long LockDuty { get; private set; }

        public FTaskFlowLock(int flowLimit, CoroutineLockComponent coroutineLockComponent, long lockDuty)
        {
            _idSem = new SemaphoreSlim[flowLimit];
            for (int i = 0; i < flowLimit; i++)
                _idSem[i] = new SemaphoreSlim(1, 1);

            CoroutineLockComponent = coroutineLockComponent;
            Scene = coroutineLockComponent.Scene;

            FTaskFlowLimit = flowLimit;
            LockDuty = lockDuty;
        }

        /// <summary>
        /// 串行等待某个Id。且同时会总体限流: 超过锁所设定上限数量值的并发FTask等到有空余任务位后再执行。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async FTask<WaitCoroutineLock> Wait(long waitForId, string tag = null, int timeOut = 10000)
        {
            // 标记进入等待队列
            Interlocked.Increment(ref _waitingCount);

            long idIndex = (waitForId & long.MaxValue) % FTaskFlowLimit;

            bool entered = false;

            try
            {
                // 按 id 串行等待
                if (!await _idSem[idIndex].WaitAsync(timeOut))
                {
                    throw new TimeoutException($"[FlowLock] timeout Id={waitForId} tag={tag ?? "null"}");
                }

                entered = true;

                // 等待结束 -> 进入临界区
                Interlocked.Decrement(ref _waitingCount);
                Interlocked.Increment(ref _activeCount);

                return CoroutineLockComponent.WaitCoroutineLockPool.Rent(this, ref idIndex, tag, timeOut);
            }
            catch
            {
                // 如果还在等待阶段就失败，需要回退 waitingCount
                if (!entered)
                    Interlocked.Decrement(ref _waitingCount);

                throw;
            }
        }

        /// <summary>
        /// 限流: 超过锁所设定上限数量值的并发FTask等到有空余任务位后再执行。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async FTask<WaitCoroutineLock> WaitIfTooMuch(string tag = null, int timeOut = 10000)
        {
            // 标记进入等待队列
            Interlocked.Increment(ref _waitingCount);

            bool entered = false;

            try
            {
                long idx = (Interlocked.Increment(ref loopingNumber) & long.MaxValue) % FTaskFlowLimit;

                if (!await _idSem[idx].WaitAsync(timeOut))
                {
                    throw new TimeoutException($"[FlowLock] timeout tag={tag ?? "null"}");
                }

                entered = true;

                // 等待结束 -> 进入临界区
                Interlocked.Decrement(ref _waitingCount);
                Interlocked.Increment(ref _activeCount);

                return CoroutineLockComponent.WaitCoroutineLockPool.Rent(this, ref idx, tag, timeOut);
            }
            catch
            {
                if (!entered)
                    Interlocked.Decrement(ref _waitingCount);

                throw;
            }
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Release(long waitingKey)
        {
            int idIndex = (int)waitingKey;
            if (idIndex < 0 || idIndex >= FTaskFlowLimit)
            {
                Log.Error($"FTaskFlowLock.Release: invalid waitingKey {waitingKey}");
                return;
            }

            // active count 自减
            int newVal = Interlocked.Decrement(ref _activeCount);
            if (newVal < 0)
            {
                Log.Error($"FTaskFlowLock.Release: _activeCount became negative ({newVal}). Resetting to 0.");
                Interlocked.Exchange(ref _activeCount, 0);
            }

            try
            {
                _idSem[idIndex].Release();
            }
            catch (SemaphoreFullException)
            {
                Log.Error($"FTaskFlowLock.Release: id lock {idIndex} release twice?");
            }
        }

        public FTaskFlowLock Reset(CoroutineLockComponent coroutineLockComponent, long newLockDuty)
        {
            Scene = coroutineLockComponent.Scene;
            CoroutineLockComponent = coroutineLockComponent;
            LockDuty = newLockDuty;

            Interlocked.Exchange(ref _activeCount, 0);
            Interlocked.Exchange(ref _waitingCount, 0);

            EnsureSemaphoresRelease();
            return this;
        }

        public FTaskFlowLock ResizeLimit(int updateLimit)
        {
            // ❗禁止运行时 resize（否则会丢锁）
            if (Volatile.Read(ref _activeCount) != 0 || Volatile.Read(ref _waitingCount) != 0)
            {
                throw new InvalidOperationException("Cannot resize FTaskFlowLock while in use");
            }

            FTaskFlowLimit = updateLimit;

            if (updateLimit > _idSem.Length)
            {
                _idSem = new SemaphoreSlim[updateLimit];
                for (int i = 0; i < updateLimit; i++)
                    _idSem[i] = new SemaphoreSlim(1, 1);
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSemaphoresRelease()
        {
            // 仅允许在完全空闲状态调用
            if (Volatile.Read(ref _activeCount) != 0 || Volatile.Read(ref _waitingCount) != 0)
            {
                Log.Error("EnsureSemaphoresRelease called while in use!");
                return;
            }

            for (int i = 0; i < _idSem.Length; i++)
            {
                var sem = _idSem[i];
                while (sem.CurrentCount < 1)
                {
                    try { sem.Release(); }
                    catch (SemaphoreFullException)
                    {
                        break;
                    }
                }
            }
        }

        public async FTask<FTaskFlowLock> CleanAsync()
        {
            while ((Volatile.Read(ref _activeCount) | Volatile.Read(ref _waitingCount)) != 0)
            {
                await Task.Delay(1);
            }

            Scene = null;
            CoroutineLockComponent = null;
            return this;
        }

        public async ValueTask DisposeAsync()
        {
            await CleanAsync();

            foreach (var sem in _idSem)
            {
                try { sem.Dispose(); } catch { }
            }

            _idSem = null;
        }
    }
}