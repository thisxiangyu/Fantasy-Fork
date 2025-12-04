using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy
{
    [DbSet(IsEmbedded = true, IsAsBytes = true)] ///嵌入式(二进制)
    public class ComponentD_AsEmbeddedBytes : Entity, IDbSet
    {
        public DbSetOptions DbSetOpts => new() { IsEmbedded = true , IsAsBytes = true};///嵌入式(二进制)

        public int EmbeddedTestInt { get; set; }
        public string? EmbeddedTestInfo { get; set; } = "这个实体被嵌入了";
    }
}