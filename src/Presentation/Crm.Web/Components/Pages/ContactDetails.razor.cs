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

    public partial class ContactDetails
    {
        [Parameter] 
        public Guid Id { get; set; }

        [Inject] 
        IContactService Service { get; set; } = default!;

        [Inject]
        IAttachmentService Attachments { get; set; } = default!;

        Contact? _contact;
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
                _contact = await Service.GetByIdAsync(Id);
                _attachments = (await Attachments.GetForAsync(RelatedToType.Contact, Id)).ToList();
            }
            catch
            {
                _contact = null;
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task SaveAsync()
        {
            if (_contact is null)
            {
                return;
            }

            await Service.UpsertAsync(_contact);
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
            await Attachments.UploadAsync(stream, file.Name, file.ContentType ?? "application/octet-stream", RelatedToType.Contact, Id);
            _attachments = (await Attachments.GetForAsync(RelatedToType.Contact, Id)).ToList();
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
            _attachments = (await Attachments.GetForAsync(RelatedToType.Contact, Id)).ToList();
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