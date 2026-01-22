namespace Crm.UI.Components;

public sealed record ActivityTimelineItem(
    string Icon,
    string Title,
    string? Subtitle = null,
    DateTime? WhenUtc = null,
    string? Status = null,
    string? LinkHref = null,
    string? LinkLabel = null,
    Guid? Id = null);
