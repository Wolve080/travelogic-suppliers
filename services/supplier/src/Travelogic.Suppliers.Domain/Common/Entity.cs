namespace Travelogic.Suppliers.Domain.Common;

/// <summary>Base type for anything with an identity that outlives its attribute values.</summary>
public abstract class Entity
{
    protected Entity(Guid id) => Id = id;

    // Required by EF Core for materialisation.
    protected Entity() { }

    public Guid Id { get; private init; }
}
