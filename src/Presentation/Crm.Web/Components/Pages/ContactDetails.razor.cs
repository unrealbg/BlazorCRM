namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Web.Components;

    using Microsoft.AspNetCore.Components;

    using System;
    using System.Threading.Tasks;

    public partial class ContactDetails
    {
        [Parameter] 
        public Guid Id { get; set; }

        [Inject] 
        IContactService Service { get; set; } = default!;

        Contact? _contact;
        bool _loading = true;
        Modal _editModal = default!;

        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            try
            {
                _contact = await Service.GetByIdAsync(Id);
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
    }
}