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

    public partial class CompanyDetails
    {
        [Parameter] 
        public Guid Id { get; set; }

        [Inject] 
        ICompanyService Service { get; set; } = default!;

        [Inject]
        IAttachmentService Attachments { get; set; } = default!;

        Company? _company;
        bool _loading = true;
        Modal _editModal = default!;
        List<Attachment> _attachments = new();
        ConfirmModal _confirm = default!;
        Guid _pendingAttachmentId;

        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            try
            {
                _company = await Service.GetByIdAsync(Id);
                _attachments = (await Attachments.GetForAsync(RelatedToType.Company, Id)).ToList();
            }
            catch
            {
                _company = null;
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task SaveAsync()
        {
            if (_company is null)
            {
                return;
            }

            await Service.UpsertAsync(_company);
            _editModal.Hide();
        }

        private async Task OnUpload(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file is null)
            {
                return;
            }

            using var stream = file.OpenReadStream(long.MaxValue);
            await Attachments.UploadAsync(stream, file.Name, file.ContentType ?? "application/octet-stream", RelatedToType.Company, Id);
            _attachments = (await Attachments.GetForAsync(RelatedToType.Company, Id)).ToList();
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
            _attachments = (await Attachments.GetForAsync(RelatedToType.Company, Id)).ToList();
        }

        static string FormatSize(long size)
        {
            if (size < 1024)
            {
                return $"{size} B";
            }

            if (size < 1024 * 1024)
            {
                return $"{size / 1024d:0.#} KB";
            }

            if (size < 1024 * 1024 * 1024)
            {
                return $"{size / (1024d * 1024d):0.#} MB";
            }

            return $"{size / (1024d * 1024d * 1024d):0.#} GB";
        }
    }
}