namespace Crm.Web.Infrastructure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Crm.Application.Common.Multitenancy;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.OutputCaching;

    public sealed class TenantScopedOutputCachePolicy : IOutputCachePolicy
    {
        private readonly TimeSpan _duration;
        private readonly string[] _varyByQueryKeys;

        public TenantScopedOutputCachePolicy(TimeSpan duration, params string[] varyByQueryKeys)
        {
            _duration = duration;
            _varyByQueryKeys = varyByQueryKeys ?? Array.Empty<string>();
        }

        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
        {
            if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
            {
                context.EnableOutputCaching = false;
                return ValueTask.CompletedTask;
            }

            var accessor = context.HttpContext.RequestServices.GetRequiredService<ITenantContextAccessor>();
            var tenant = accessor.Current;
            if (tenant is null)
            {
                context.EnableOutputCaching = false;
                return ValueTask.CompletedTask;
            }

            context.EnableOutputCaching = true;
            context.AllowCacheLookup = true;
            context.AllowCacheStorage = true;
            context.AllowLocking = true;

            context.ResponseExpirationTimeSpan = _duration;
            context.CacheVaryByRules.VaryByValues["tenant"] = tenant.TenantSlug;

            if (_varyByQueryKeys.Length > 0)
            {
                context.CacheVaryByRules.QueryKeys = _varyByQueryKeys;
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
