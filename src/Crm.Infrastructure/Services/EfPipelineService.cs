namespace Crm.Infrastructure.Services
{
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Infrastructure.Persistence;
    using Microsoft.EntityFrameworkCore;

    public class EfPipelineService : IPipelineService
    {
        private readonly CrmDbContext _db;

        public EfPipelineService(CrmDbContext db) => _db = db;

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
            return stage;
        }
    }
}
