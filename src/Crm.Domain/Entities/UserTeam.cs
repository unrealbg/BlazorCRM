namespace Crm.Domain.Entities
{
    public class UserTeam : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid TeamId { get; set; }
    }
}
