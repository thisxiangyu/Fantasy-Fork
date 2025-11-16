using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    [DbSet(Relationship = ToParentIs.Component)]
    public class ComponentA : Entity, IFollowSerialization
    {
  
    }
}
