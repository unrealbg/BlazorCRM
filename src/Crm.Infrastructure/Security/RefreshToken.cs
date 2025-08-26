namespace Crm.Infrastructure.Security
{
    public sealed class RefreshToken
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = default!;

        public string TokenHash { get; set; } = default!;

        public DateTime ExpiresAtUtc { get; set; }

        public bool IsRevoked { get; set; }

        public string? ReplacedByHash { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
