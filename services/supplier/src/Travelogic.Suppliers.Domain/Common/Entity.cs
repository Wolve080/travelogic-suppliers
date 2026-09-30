namespace Travelogic.Suppliers.Domain.Common;

public abstract class Entity
{
    protected Entity(Guid id) => Id = id;

    // EF Core
    protected Entity() { }

    public Guid Id { get; private init; }
}
