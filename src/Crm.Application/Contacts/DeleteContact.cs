namespace Crm.Application.Contacts
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record DeleteContact(Guid Id) : IRequest<bool>;

    public sealed class DeleteContactValidator : AbstractValidator<DeleteContact>
    {
        public DeleteContactValidator() { RuleFor(x => x.Id).NotEmpty(); }
    }

    public sealed class DeleteContactHandler : IRequestHandler<DeleteContact, bool>
    {
        private readonly IContactService _svc;
        public DeleteContactHandler(IContactService svc) => _svc = svc;

        public Task<bool> Handle(DeleteContact r, CancellationToken ct)
            => _svc.DeleteAsync(r.Id, ct);
    }
}
