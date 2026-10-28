namespace EskaCMS.Infrastructure.Models
{
    public interface IEntityWithTypedId<TId>
    {
        TId Id { get; }
    }
}
