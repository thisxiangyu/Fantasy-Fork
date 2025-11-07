using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    [DbSet(Relationship = ToParentIs.JustLinking)]
    public class Child :Entity, IMultiAppended, IFollowCRUD, IDbSetRef<ComponentA>
    {
       
    }
}
