namespace Crm.Infrastructure.Security
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Crm.Contracts.Auth;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;

    public sealed class JwtTokenService : ITokenService
    {
        private readonly IConfiguration _cfg;
        public JwtTokenService(IConfiguration cfg) => _cfg = cfg;

        public LoginResponse CreateToken(string userId, string userName, Guid tenantId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(8);

            var claims = new[]
                             {
                                 new Claim(ClaimTypes.NameIdentifier, userId),
                                 new Claim(ClaimTypes.Name, userName),
                                 new Claim("tenant", tenantId.ToString())
                             };

            var token = new JwtSecurityToken(
                issuer: _cfg["Jwt:Issuer"],
                audience: _cfg["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
