namespace Crm.Domain.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }

        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");
    }
}
