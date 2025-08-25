namespace Crm.Application.Contacts
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Application.Common.Behaviors;
    using Crm.Application.Security;

    [RequiresPermission(Permissions.Contacts_Write)]
    [RequiresPermission(Permissions.Contacts_Import)]
    public sealed record CreateContact(string FirstName, string LastName, string? Email, string? Phone, string? Position, Guid? CompanyId, string[]? Tags) : IRequest<Guid>;

    public sealed class CreateContactValidator : AbstractValidator<CreateContact>
    {
        public CreateContactValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        }
    }

    public sealed class CreateContactHandler : IRequestHandler<CreateContact, Guid>
    {
        private readonly IContactService _svc;
        public CreateContactHandler(IContactService svc) => _svc = svc;

        public async Task<Guid> Handle(CreateContact r, CancellationToken ct)
        {
            var contact = new Contact { Id = Guid.Empty, FirstName = r.FirstName, LastName = r.LastName, Email = r.Email, Phone = r.Phone, Position = r.Position, CompanyId = r.CompanyId, Tags = (r.Tags ?? Array.Empty<string>()).ToList() };
            var saved = await _svc.UpsertAsync(contact, ct);
            return saved.Id;
        }
    }
}
