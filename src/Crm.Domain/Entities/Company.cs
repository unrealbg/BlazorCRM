namespace Crm.Domain.Entities
{
    public class Company : BaseEntity
    {
        public required string Name { get; set; }

        public string? Industry { get; set; }

        public string? Address { get; set; }

        public List<string> Tags { get; set; } = new();
    }
}
