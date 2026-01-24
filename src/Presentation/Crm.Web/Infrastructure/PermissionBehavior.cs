namespace Crm.Web.Infrastructure;

using System.Reflection;
using Crm.Application.Common.Behaviors;
using Crm.Application.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

public sealed class PermissionBehavior<TReq, TRes> : IPipelineBehavior<TReq, TRes>
    where TReq : notnull
{
    private readonly IPermissionEvaluator _evaluator;
    private readonly IHttpContextAccessor _ctx;
    public PermissionBehavior(IPermissionEvaluator evaluator, IHttpContextAccessor ctx)
    {
        _evaluator = evaluator;
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

            if (!_evaluator.HasPermission(user, a.Policy))
            {
                throw new PermissionDeniedException(a.Policy, isAuthenticated: true);
            }
        }

        return await next();
    }
}
