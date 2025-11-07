using System.ComponentModel.DataAnnotations.Schema;
using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy
{
    [DbSet(Relationship = ToParentIs.JustLinking)]
    public class Grandchild : Entity, IFollowCRUD
    {
        public int TestIntField;

        public int TestStringField;

        [NotMapped]
        public int NotMapped;
    }
}