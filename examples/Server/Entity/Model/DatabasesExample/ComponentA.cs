using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fantasy
{
    [DbSet]
    public class ComponentA : Entity, IDbSet
    {
        public DbSetOptions? DbSetOpts => null;

        public string A { get; set; } = "It is ComponentA";

    }
}
