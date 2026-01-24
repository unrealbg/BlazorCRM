namespace Crm.Infrastructure.Security
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using Crm.Contracts.Auth;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;

    public sealed class JwtTokenService : ITokenService
    {
        private readonly IConfiguration _cfg;
        private static string Hash(string value)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }

        public JwtTokenService(IConfiguration cfg) => _cfg = cfg;

        public LoginResponse CreateToken(string userId, string userName, Guid tenantId, string tenantSlug, IEnumerable<string> roles)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(8);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim("tenant", tenantId.ToString()),
                new Claim("tenant_slug", tenantSlug ?? string.Empty)
            };

            foreach (var role in roles ?? Array.Empty<string>())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _cfg["Jwt:Issuer"],
                audience: _cfg["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, refresh);
        }

        public static string HashRefresh(string refreshToken) => Hash(refreshToken);
    }
}
