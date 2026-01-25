namespace Crm.Contracts.Search
{
    public sealed record SearchGroupResultDto(
        IReadOnlyList<SearchResultDto> Companies,
        IReadOnlyList<SearchResultDto> Contacts,
        IReadOnlyList<SearchResultDto> Deals);
}