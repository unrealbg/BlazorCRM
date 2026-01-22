namespace Crm.Infrastructure.Services
{
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    public class EfPipelineService : IPipelineService
    {
        private readonly CrmDbContext _db;
        private readonly IMemoryCache _cache;
        private static readonly string StageMapCacheKey = "stage-map";

        public EfPipelineService(CrmDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IEnumerable<Pipeline>> GetPipelinesAsync(Guid? tenantId = null, CancellationToken ct = default)
        {
            IQueryable<Pipeline> q = _db.Pipelines.AsNoTracking();
            if (tenantId is Guid tid)
            {
                q = q.Where(p => p.TenantId == tid);
            }

            return await q.OrderBy(p => p.Name).ToListAsync(ct);
        }

        public async Task<IEnumerable<Stage>> GetStagesAsync(Guid pipelineId, CancellationToken ct = default)
            => await _db.Stages.AsNoTracking().Where(s => s.PipelineId == pipelineId).OrderBy(s => s.Order).ToListAsync(ct);

        public async Task<Stage?> GetStageByIdAsync(Guid stageId, CancellationToken ct = default)
            => await _db.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stageId, ct);

        public async Task<IDictionary<Guid, string>> GetStageNameMapAsync(Guid? pipelineId = null, CancellationToken ct = default)
        {
            var key = pipelineId is Guid pid ? $"{StageMapCacheKey}:{pid}" : StageMapCacheKey;
            if (_cache.TryGetValue<IDictionary<Guid, string>>(key, out var cached))
            {
                if (cached is not null)
                {
                    return cached;
                }
            }

            IQueryable<Stage> q = _db.Stages.AsNoTracking();
            if (pipelineId is Guid pid2)
            {
                q = q.Where(s => s.PipelineId == pid2);
            }

            var map = await q.Select(s => new { s.Id, s.Name }).ToDictionaryAsync(x => x.Id, x => x.Name, ct);

            _cache.Set(key, map, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            return map;
        }

        public async Task<Pipeline> UpsertPipelineAsync(Pipeline pipeline, CancellationToken ct = default)
        {
            if (pipeline.Id == Guid.Empty)
            {
                pipeline.Id = Guid.NewGuid();
                await _db.Pipelines.AddAsync(pipeline, ct);
            }
            else
            {
                var existing = await _db.Pipelines.AsTracking().FirstOrDefaultAsync(p => p.Id == pipeline.Id, ct);
                if (existing is null)
                {
                    await _db.Pipelines.AddAsync(pipeline, ct);
                }
                else
                {
                    _db.Entry(existing).CurrentValues.SetValues(pipeline);
                }
            }

            await _db.SaveChangesAsync(ct);
            _cache.Remove(StageMapCacheKey);
            return pipeline;
        }

        public async Task<Stage> UpsertStageAsync(Stage stage, CancellationToken ct = default)
        {
            if (stage.Id == Guid.Empty)
            {
                stage.Id = Guid.NewGuid();
                await _db.Stages.AddAsync(stage, ct);
            }
            else
            {
                var existing = await _db.Stages.AsTracking().FirstOrDefaultAsync(s => s.Id == stage.Id, ct);
                if (existing is null)
                {
                    await _db.Stages.AddAsync(stage, ct);
                }
                else
                {
                    _db.Entry(existing).CurrentValues.SetValues(stage);
                }
            }

            await _db.SaveChangesAsync(ct);
            _cache.Remove(StageMapCacheKey);
            _cache.Remove($"{StageMapCacheKey}:{stage.PipelineId}");
            return stage;
        }

        public async Task<bool> DeletePipelineAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Pipelines.FindAsync(new object?[] { id }, ct);
            if (entity is null)
            {
                return false;
            }

            _db.Pipelines.Remove(entity);
            await _db.SaveChangesAsync(ct);
            _cache.Remove(StageMapCacheKey);
            return true;
        }

        public async Task<bool> DeleteStageAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Stages.FindAsync(new object?[] { id }, ct);
            if (entity is null)
            {
                return false;
            }

            _db.Stages.Remove(entity);
            await _db.SaveChangesAsync(ct);
            _cache.Remove(StageMapCacheKey);
            return true;
        }
    }
}
