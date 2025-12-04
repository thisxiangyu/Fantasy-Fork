using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy
{
    [DbSet]
    public class ComponentB : Entity, IDbSet
    {
        public DbSetOptions? DbSetOpts => null;

        public string B { get; set; } = "It is ComponentB";
    }
}
