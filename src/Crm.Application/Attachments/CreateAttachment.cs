namespace Crm.Application.Attachments
{
    using FluentValidation;
    using MediatR;
    using Crm.Application.Services;
    using Crm.Domain.Enums;

    public sealed record CreateAttachment(Stream Content, string FileName, string ContentType, RelatedToType RelatedTo, Guid? RelatedId) : IRequest<Guid>;

    public sealed class CreateAttachmentValidator : AbstractValidator<CreateAttachment>
    {
        public CreateAttachmentValidator()
        {
            RuleFor(x => x.FileName).NotEmpty();
            RuleFor(x => x.ContentType).NotEmpty();
        }
    }

    public sealed class CreateAttachmentHandler : IRequestHandler<CreateAttachment, Guid>
    {
        private readonly IAttachmentService _svc;
        public CreateAttachmentHandler(IAttachmentService svc) => _svc = svc;

        public async Task<Guid> Handle(CreateAttachment r, CancellationToken ct)
        {
            using var content = r.Content;
            var saved = await _svc.UploadAsync(content, r.FileName, r.ContentType, r.RelatedTo, r.RelatedId, ct);
            return saved.Id;
        }
    }
}
