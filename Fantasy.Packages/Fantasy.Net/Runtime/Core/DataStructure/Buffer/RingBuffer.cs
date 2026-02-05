using System;

namespace Fantasy.DataStructure.Buffer
{
    /// <summary>
    /// 简易环形缓冲
    /// </summary>
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private int _index;
        private int _count;

        public RingBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _index = 0;
            _count = 0;
        }

        public RingBuffer(uint capacity)
        {
            _buffer = new T[capacity];
            _index = 0;
            _count = 0;
        }

        public void Add(T value)
        {
            _buffer[_index] = value;
            // 使用位运算代替 % (如果 capacity 是 2 的幂，性能更高，否则保持现状即可)
            _index = (_index + 1) % _buffer.Length;
            if (_count < _buffer.Length)
                _count++;
        }

        public int Count => _count;

        public int WriteIndex => _index;

        public int Capacity => _buffer.Length;

        // 仅仅判断数量是否超标，不需要管顺序
        public bool IsFull => _count >= _buffer.Length;

        // Note: TODO 如果后续要分析行为，再提供索引器
        public T this[int i]
        {
            get
            {
                if (i < 0 || i >= _count) throw new IndexOutOfRangeException();
                // 逻辑位置映射到物理位置：旧数据在 index 之后，新数据在 index 之前
                int realIndex = (_count < _buffer.Length) ? i : (_index + i) % _buffer.Length;
                return _buffer[realIndex];
            }
        }

        public void Clear()
        {
            _index = 0;
            _count = 0;
        }
    }
}
