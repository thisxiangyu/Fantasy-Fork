using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy.Database.Attributes
{
    /// <summary>
    /// DbSet 相关属性名 (这些通常是作为影子属性)
    /// </summary>
    public static class DbSetProperty
    {
        /// <summary>
        /// 引用类型
        /// </summary>
        public const string RefType = "RefType";
        /// <summary>
        /// 引用Id
        /// </summary>
        public const string RefId = "RefId";
    }
}
