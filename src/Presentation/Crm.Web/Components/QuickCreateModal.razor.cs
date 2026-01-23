namespace Crm.Web.Components
{
    using Crm.Application.Companies;

    using MediatR;

    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using System;
    using System.Threading.Tasks;

    public partial class QuickCreateModal
    {
        [Inject] 
        IJSRuntime JS { get; set; } = default!;

        [Inject] 
        IMediator Mediator { get; set; } = default!;

        public string Type { get; set; } = "Company";
        public string Title { get; set; } = string.Empty;
        private bool _busy;

        [JSInvokable]
        public Task Show() => JS.InvokeVoidAsync("openModal", "modalQuickCreate").AsTask();

        private Task Hide() => JS.InvokeVoidAsync("closeModal", "modalQuickCreate").AsTask();

        private async Task Create()
        {
            if (_busy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                await JS.InvokeVoidAsync("showToast", "Title is required", "error");
                return;
            }

            try
            {
                _busy = true;
                if (string.Equals(Type, "Company", StringComparison.OrdinalIgnoreCase))
                {
                    var id = await Mediator.Send(new CreateCompany(Title.Trim(), null, null));
                    await JS.InvokeVoidAsync("showToast", $"Company created: {Title}");
                    await JS.InvokeVoidAsync("publish", "company:created", new { id });
                }
                else
                {
                    await JS.InvokeVoidAsync("showToast", "Quick create supports Company only for now");
                }

                Title = string.Empty;
                await Hide();
            }
            catch (Exception ex)
            {
                await JS.InvokeVoidAsync("showToast", ex.Message, "error");
            }
            finally
            {
                _busy = false;
                StateHasChanged();
            }
        }
    }
}