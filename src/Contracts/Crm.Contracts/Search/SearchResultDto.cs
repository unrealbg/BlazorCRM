namespace Crm.Contracts.Search
{
    public sealed record SearchResultDto(
        string Type,
        Guid Id,
        string Title,
        string? Subtitle,
        double Rank,
        string Url);
}
