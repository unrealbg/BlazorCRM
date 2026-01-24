namespace Crm.Domain.Common
{
    public interface IHasCreatedAt
    {
        DateTime CreatedAtUtc { get; set; }
    }
}
