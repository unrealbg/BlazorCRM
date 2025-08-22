namespace Crm.Contracts.Auth
{
    public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);
}
