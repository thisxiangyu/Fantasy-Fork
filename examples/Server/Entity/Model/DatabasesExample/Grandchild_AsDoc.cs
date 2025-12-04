using System.ComponentModel.DataAnnotations.Schema;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy
{
    [DbSet(IsAsDocument = true)] ///独立文档
    public class Grandchild_AsDoc : Entity, IDbSet
    {
        public DbSetOptions DbSetOpts => new() { IsAsDocument = true }; ///独立文档

        public int TestIntProperty { get; set; } = 5;

        public string TestStringProperty { get; set; } = "测一下";

        public int TestIntField = 10;

        public string TestStringField = "测一下";

        [NotMapped]
        public int NotMapped;
    }
}