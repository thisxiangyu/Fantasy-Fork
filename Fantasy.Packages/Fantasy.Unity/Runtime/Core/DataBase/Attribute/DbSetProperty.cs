#if FANTASY_NET

namespace Fantasy.Database.Attributes
{
    /// <summary>
    /// DbSet 相关属性名 (这些通常是作为影子属性)
    /// </summary>
    public class DbSetProperty
    {
        /// <summary>
        /// 父级引用类型
        /// </summary>
        public static string ParentType = "ParentType";
        /// <summary>
        /// 父级引用Id
        /// </summary>
        public static string ParentId = "ParentId";
        /// <summary>
        /// 独一份的子实体存储字段
        /// </summary>
        public static string JsonSingle = "_s";
        /// <summary>
        /// 多份的子实体存储属性
        /// </summary>
        public static string JsonMulti = "_m";
        /// <summary>
        /// 独一份的子实体字节存储字段
        /// </summary>
        public static string BytesSingle = "_bytes_s";
        /// <summary>
        /// 多份的子实体字节存储属性
        /// </summary>
        public static string BytesMulti = "_bytes_m";
        /// <summary>
        /// 多实体查询的行按照什么属性拆分, 默认为Id
        /// </summary>
        public static string MultiEntitiesRowSplitOn = "Id";
        /// <summary>
        /// 字节式文档
        /// </summary>
        public static string DocAsBytes = "Bytes";
        /// <summary>
        /// Json式文档
        /// </summary>
        public static string DocAsJson = "Json";
    }
}
#endif