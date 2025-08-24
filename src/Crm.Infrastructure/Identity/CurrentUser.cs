namespace Crm.Infrastructure.Identity
{
    using System.Security.Claims;

    using Crm.Application.Common.Abstractions;

    using Microsoft.AspNetCore.Http;

    public sealed class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _http;

        public CurrentUser(IHttpContextAccessor http) => _http = http;

        public string? UserId => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? CorrelationId => _http.HttpContext?.TraceIdentifier;
    }
}
