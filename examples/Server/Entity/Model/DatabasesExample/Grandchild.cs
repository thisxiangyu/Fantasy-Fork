using System.ComponentModel.DataAnnotations.Schema;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy
{
    [DbSet(Relationship = ToParentIs.Child,IsAsDocument = true)] ///独立文档
    public class Grandchild : Entity, IFollowSerialization
    {
        public int TestIntField;

        public int TestStringField;

        [NotMapped]
        public int NotMapped;
    }
}