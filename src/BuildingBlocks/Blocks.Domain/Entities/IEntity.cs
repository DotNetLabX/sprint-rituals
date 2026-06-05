namespace Blocks.Domain.Entities;

public interface IEntity<TPrimaryKey>
{
    TPrimaryKey Id { get; }
}
