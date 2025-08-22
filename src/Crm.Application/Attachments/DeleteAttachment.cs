namespace Crm.Application.Attachments
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;

    public sealed record DeleteAttachment(Guid Id) : IRequest<bool>;

    public sealed class DeleteAttachmentValidator : AbstractValidator<DeleteAttachment>
    {
        public DeleteAttachmentValidator() => RuleFor(x => x.Id).NotEmpty();
    }

    public sealed class DeleteAttachmentHandler : IRequestHandler<DeleteAttachment, bool>
    {
        private readonly IAttachmentService _svc;
        public DeleteAttachmentHandler(IAttachmentService svc) => _svc = svc;

        public Task<bool> Handle(DeleteAttachment r, CancellationToken ct) => _svc.DeleteAsync(r.Id, ct);
    }
}
