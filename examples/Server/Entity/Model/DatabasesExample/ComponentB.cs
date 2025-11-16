using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy
{
    [DbSet(Relationship = ToParentIs.Component)]
    public class ComponentB : Entity, IFollowSerialization
    {

    }
}
