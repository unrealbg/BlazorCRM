namespace Crm.Contracts.Auth
{
    public sealed record RefreshRequest(string RefreshToken);

    public sealed record LogoutRequest(string? RefreshToken);
}
