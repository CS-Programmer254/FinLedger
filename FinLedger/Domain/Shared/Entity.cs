namespace FinLedger.Domain.Shared;

/// <summary>
/// Base class for all domain entities - provides identity and equality
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; }

    protected Entity(TId id)
    {
        if (Equals(id, default))
            throw new ArgumentException("Id cannot be default value", nameof(id));
        Id = id;
    }

    protected Entity() { }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> entity && Equals(entity);

    public bool Equals(Entity<TId>? other) =>
        other is not null && Id.Equals(other.Id);

    public override int GetHashCode() =>
        Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !Equals(left, right);
}

