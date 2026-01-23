namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Web.Components;

    using Microsoft.AspNetCore.Components;

    using System;
    using System.Threading.Tasks;

    public partial class CompanyDetails
    {
        [Parameter] 
        public Guid Id { get; set; }

        [Inject] 
        ICompanyService Service { get; set; } = default!;

        Company? _company;
        bool _loading = true;
        Modal _editModal = default!;

        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            try
            {
                _company = await Service.GetByIdAsync(Id);
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
    }
}