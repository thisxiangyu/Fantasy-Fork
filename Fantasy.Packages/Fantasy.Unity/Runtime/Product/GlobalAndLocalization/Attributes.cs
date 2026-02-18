using System;

namespace Fantasy.GlobalAndLocalization
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public sealed class 区域TagAttribute : Attribute
    {
        public 区域码 RegionCode { get; private set; }

        public 区域TagAttribute(区域码 地区码)
        {
            RegionCode = 地区码;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class 文化大区Attribute : Attribute
    {
        public 区域码 大区 { get; private set; }
        public 文化大区Attribute(区域码 大区)
        {
            this.大区 = 大区;
        }
    }
    
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class 地理大区Attribute : Attribute
    {
        public 区域码 大区 { get; private set; }
        public 地理大区Attribute(区域码 大区)
        {
            this.大区 = 大区;
        }
    }
    
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class 大洲Attribute : Attribute
    {
        public 区域码 大区 { get; private set; }
        public 大洲Attribute(区域码 大区)
        {
            this.大区 = 大区;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class 语言码Attribute : Attribute
    {
        public string 语言码 { get; private set; }
        public uint 语言顺序 { get; private set; } = 0; // 在多语言的时候需要设置顺序
        public 语言码Attribute(string 语言码, uint 语言顺序 = 0)
        {
            this.语言码 = 语言码;
            this.语言顺序 = 语言顺序;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class 简写Attribute : Attribute
    {
        public string 简写 { get; private set; }
        public 简写Attribute(string 简写)
        {
            this.简写 = 简写;
        }
    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class 共用区码Attribute : Attribute
    {
        public 区域码 共用者 { get; private set; }
        public 共用区码Attribute(区域码 共用者)
        {
            this.共用者 = 共用者;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class 特殊标记Attribute : Attribute
    {

    }
}