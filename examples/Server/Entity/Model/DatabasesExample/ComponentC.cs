using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Microsoft.EntityFrameworkCore;

namespace Fantasy
{
    [DbSet(Relationship = ToParentIs.Component,IsEmbedded = true)] ///嵌入式(文档)
    public class ComponentC : Entity, IFollowSerialization
    {
        public int Int_C { get; set; }
        public string? String_C { get; set; }
    }
}
