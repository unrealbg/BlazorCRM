namespace Crm.Application.Contacts
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record UpdateContact(Guid Id, string FirstName, string LastName, string? Email, string? Phone, string? Position, Guid? CompanyId, string[]? Tags) : IRequest<bool>;

    public sealed class UpdateContactValidator : AbstractValidator<UpdateContact>
    {
        public UpdateContactValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        }
    }

    public sealed class UpdateContactHandler : IRequestHandler<UpdateContact, bool>
    {
        private readonly IContactService _svc;
        public UpdateContactHandler(IContactService svc) => _svc = svc;

        public async Task<bool> Handle(UpdateContact r, CancellationToken ct)
        {
            var current = await _svc.GetByIdAsync(r.Id, ct);
            current.FirstName = r.FirstName;
            current.LastName = r.LastName;
            current.Email = r.Email;
            current.Phone = r.Phone;
            current.Position = r.Position;
            current.CompanyId = r.CompanyId;
            current.Tags = (r.Tags ?? Array.Empty<string>()).ToList();
            await _svc.UpsertAsync(current, ct);
            return true;
        }
    }
}
