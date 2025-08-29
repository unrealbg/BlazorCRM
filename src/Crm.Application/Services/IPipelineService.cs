namespace Crm.Application.Services
{
    using Crm.Domain.Entities;

    public interface IPipelineService
    {
        Task<IEnumerable<Pipeline>> GetPipelinesAsync(Guid? tenantId = null, CancellationToken ct = default);

        Task<IEnumerable<Stage>> GetStagesAsync(Guid pipelineId, CancellationToken ct = default);

        Task<Stage?> GetStageByIdAsync(Guid stageId, CancellationToken ct = default);

        Task<IDictionary<Guid, string>> GetStageNameMapAsync(Guid? pipelineId = null, CancellationToken ct = default);

        Task<Pipeline> UpsertPipelineAsync(Pipeline pipeline, CancellationToken ct = default);

        Task<Stage> UpsertStageAsync(Stage stage, CancellationToken ct = default);

        Task<bool> DeletePipelineAsync(Guid id, CancellationToken ct = default);

        Task<bool> DeleteStageAsync(Guid id, CancellationToken ct = default);
    }
}
