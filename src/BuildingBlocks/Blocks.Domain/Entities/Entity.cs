namespace Blocks.Domain.Entities;

public abstract class Entity<TPrimaryKey> : IEntity<TPrimaryKey>
{
    public TPrimaryKey Id { get; set; } = default!;

    public bool IsNew => EqualityComparer<TPrimaryKey>.Default.Equals(Id, default);

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TPrimaryKey> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (IsNew || other.IsNew) return false;
        return EqualityComparer<TPrimaryKey>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TPrimaryKey>? left, Entity<TPrimaryKey>? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity<TPrimaryKey>? left, Entity<TPrimaryKey>? right)
        => !(left == right);
}
