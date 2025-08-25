namespace Crm.Application.Contacts
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Application.Common.Behaviors;
    using Crm.Application.Security;

    [RequiresPermission(Permissions.Contacts_Write)]
    public sealed record UpdateContact(Guid Id, string FirstName, string LastName, string? Email, string? Phone, string? Position, Guid? CompanyId, string[]? Tags) : IRequest<bool>;

    public sealed class UpdateContactValidator : AbstractValidator<UpdateContact>
    {
        public UpdateContactValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
        }
    }

    public sealed class UpdateContactHandler : IRequestHandler<UpdateContact, bool>
    {
        private readonly IContactService _svc;
        public UpdateContactHandler(IContactService svc) => _svc = svc;

        public async Task<bool> Handle(UpdateContact r, CancellationToken ct)
        {
            var c = new Contact { Id = r.Id, FirstName = r.FirstName, LastName = r.LastName, Email = r.Email, Phone = r.Phone, Position = r.Position, CompanyId = r.CompanyId, Tags = (r.Tags ?? Array.Empty<string>()).ToList() };
            await _svc.UpsertAsync(c, ct);
            return true;
        }
    }
}
