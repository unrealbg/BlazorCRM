namespace Crm.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        public required string Name { get; set; }

        public required string Slug { get; set; }

        public string? SettingsJson { get; set; }
    }
}
