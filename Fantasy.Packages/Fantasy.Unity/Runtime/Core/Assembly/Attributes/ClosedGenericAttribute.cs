using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Fantasy.Helper;

namespace Fantasy.Attributes
{
    /// <summary>
    /// 动态预注册闭合泛型类的Attribute。
    /// 一般来说, 框架在初始化阶段将提前生成闭合的泛型。 
    /// 但是, 如果一些泛型是动态性质的, 在任意类或枚举类打上这个标签, 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, Inherited = false, AllowMultiple = true)]
    public class ClosedGenericAttribute : Attribute
    {
        /// <summary>
        /// 闭合泛型
        /// </summary>
        public Type? theClosed { get; }
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="theClosedGeneric"></param>
        public ClosedGenericAttribute(Type theClosedGeneric)
        {
            theClosed = theClosedGeneric;
        }
    }

    //TODO 自动分析泛型闭合
    ///// <summary>
    ///// 自动分析闭合的泛型
    ///// 注意 : 这个Attribute 必须打在泛型类的定义上, 不允许随意打到任意位置, 否则无效
    ///// </summary>
    //[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    //public class AutoCloseGenericOnLoadAttribute : Attribute
    //{

    //}
}
