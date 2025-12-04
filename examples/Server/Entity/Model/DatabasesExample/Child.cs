using Fantasy.Database.Attributes;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace Fantasy
{
    [DbSet]
    public class Child :Entity, IMultiAppended, IDbSet
    {
        public DbSetOptions? DbSetOpts => null;
        public int Child_Int { get; set; } = 999;
    }
}
