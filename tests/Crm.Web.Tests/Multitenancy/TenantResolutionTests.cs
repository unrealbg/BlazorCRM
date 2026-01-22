namespace Crm.Web.Tests.Multitenancy
{
    using Crm.Application.Common.Multitenancy;
    using Crm.Infrastructure.Multitenancy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using System.Security.Claims;

    public class TenantResolutionTests
    {
        [Fact]
        public void Resolve_RequiresTenantClaim_ForAuthenticatedUser()
        {
            var defaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var options = Options.Create(new TenantOptions
            {
                DefaultTenantId = defaultTenantId,
                DefaultTenantName = "Default"
            });

            var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            accessor.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("tenant", defaultTenantId.ToString())
            }, "Test"));

            var resolver = new DefaultTenantResolver(accessor, options);
            var resolved = resolver.Resolve();
            Assert.True(resolved.IsResolved);
            Assert.Equal(defaultTenantId, resolved.TenantId);

            accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "Test"));
            var missing = resolver.Resolve();
            Assert.False(missing.IsResolved);
        }
    }
}
