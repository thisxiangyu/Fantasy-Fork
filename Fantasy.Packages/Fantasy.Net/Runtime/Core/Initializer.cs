using System;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Fantasy
{
    public static class Initializer
    {
#if FANTASY_NET
        /// <summary>
        /// Bson初始化的事件
        /// </summary>
        public static Action OnBsonInitialize;
#endif
        /// <summary>
        /// MemoryPack初始化的事件
        /// </summary>
        public static Action OnMemoryPackInitialize;
    }
}


