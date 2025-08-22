namespace Crm.Application.Common.Abstractions
{
    public interface ICurrentUser
    {
        string? UserId { get; }

        string? CorrelationId { get; }
    }
}
