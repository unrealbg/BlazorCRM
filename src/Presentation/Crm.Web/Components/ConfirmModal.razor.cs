namespace Crm.Web.Components
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using System.Threading.Tasks;

    public partial class ConfirmModal
    {
        private bool _open;

        [Parameter] 
        public string Title { get; set; } = "Confirm";

        [Parameter] 
        public string Message { get; set; } = "Are you sure?";

        [Parameter] 
        public string ConfirmText { get; set; } = "Yes";

        [Parameter] 
        public string CancelText { get; set; } = "Cancel";

        [Parameter] 
        public EventCallback OnConfirmed { get; set; }

        public void Show() 
        { 
            _open = true; StateHasChanged(); 
        }
        public void Hide() 
        { 
            _open = false; StateHasChanged(); 
        }
        private async Task Confirm() 
        { 
            await OnConfirmed.InvokeAsync(); Hide(); 
        }
        private void Cancel() => Hide();
    }
}