namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Domain.Enums;
    using Crm.Web.Components;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Forms;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class DealDetails
    {
        [Parameter] 
        public Guid Id { get; set; }

        [Inject] 
        IDealService Deals { get; set; } = default!;

        [Inject] 
        IAttachmentService Attachments { get; set; } = default!;

        [Inject] 
        IPipelineService Pipelines { get; set; } = default!;

        Deal? _deal;
        bool _loading = true;
        List<Attachment> _attachments = new();
        string? _stageName;
        Modal _editModal = default!;
        ConfirmModal _confirm = default!;
        Guid _pendingAttachmentId;

        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            try
            {
                _deal = await Deals.GetByIdAsync(Id);
                _attachments = (await Attachments.GetForAsync(RelatedToType.Deal, Id)).ToList();
                var map = await Pipelines.GetStageNameMapAsync();
                _stageName = map.TryGetValue(_deal.StageId, out var name) ? name : null;
            }
            catch
            {
                _deal = null;
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task OnUpload(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file is null)
            {
                return;
            }

            using var stream = file.OpenReadStream(long.MaxValue);
            await Attachments.UploadAsync(stream, file.Name, file.ContentType ?? "application/octet-stream", RelatedToType.Deal, Id);
            _attachments = (await Attachments.GetForAsync(RelatedToType.Deal, Id)).ToList();
        }

        private void AskDeleteAttachment(Guid id)
        {
            _pendingAttachmentId = id;
            _confirm.Show();
        }

        private async Task ConfirmDeleteAttachmentAsync()
        {
            if (_pendingAttachmentId == Guid.Empty)
            {
                return;
            }

            await Attachments.DeleteAsync(_pendingAttachmentId);
            _pendingAttachmentId = Guid.Empty;
            _attachments = (await Attachments.GetForAsync(RelatedToType.Deal, Id)).ToList();
        }

        private async Task SaveAsync()
        {
            if (_deal is null)
            {
                return;
            }

            await Deals.UpsertAsync(_deal);
            _editModal.Hide();
        }
    }
}