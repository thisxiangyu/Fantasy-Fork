using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Microsoft.EntityFrameworkCore;

namespace Fantasy
{
    [DbSet(IsEmbedded = true)] ///嵌入式(文档)
    public class ComponentC_AsEmbeddDoc : Entity, IDbSet
    {
        public DbSetOptions DbSetOpts => new() { IsEmbedded = true };///嵌入式(文档)

        public int Int_C { get; set; }
        public string? String_C { get; set; }
    }
}
