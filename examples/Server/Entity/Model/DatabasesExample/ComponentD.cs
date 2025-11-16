using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy
{
    [DbSet(Relationship = ToParentIs.Component, IsEmbedded = true, IsAsBytes = true)] ///嵌入式(二进制)
    public class ComponentD : Entity, IFollowSerialization
    {
        public int EmbeddedTestInt { get; set; }
        public string? EmbeddedTestInfo { get; set; } = "这个实体被嵌入了";
    }
}