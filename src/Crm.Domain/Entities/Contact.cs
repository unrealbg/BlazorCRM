namespace Crm.Domain.Entities
{
    public class Contact : BaseEntity
    {
        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Position { get; set; }

        public Guid? CompanyId { get; set; }

        public List<string> Tags { get; set; } = new();
    }
}
