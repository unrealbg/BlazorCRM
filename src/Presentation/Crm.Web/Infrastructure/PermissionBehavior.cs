namespace Crm.Web.Infrastructure;

using System.Reflection;
using Crm.Application.Common.Behaviors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

public sealed class PermissionBehavior<TReq, TRes> : IPipelineBehavior<TReq, TRes>
    where TReq : notnull
{
    private readonly IAuthorizationService _auth;
    private readonly IHttpContextAccessor _ctx;
    public PermissionBehavior(IAuthorizationService auth, IHttpContextAccessor ctx)
    {
        _auth = auth;
        _ctx = ctx;
    }

    public async Task<TRes> Handle(TReq request, RequestHandlerDelegate<TRes> next, CancellationToken ct)
    {
        var attrs = request.GetType().GetCustomAttributes<RequiresPermissionAttribute>(true).ToArray();
        if (attrs.Length == 0)
        {
            return await next();
        }

        var user = _ctx.HttpContext?.User;
        foreach (var a in attrs)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new PermissionDeniedException(a.Policy, isAuthenticated: false);
            }

            var result = await _auth.AuthorizeAsync(user, null, a.Policy);
            if (!result.Succeeded)
            {
                throw new PermissionDeniedException(a.Policy, isAuthenticated: true);
            }
        }

        return await next();
    }
}
