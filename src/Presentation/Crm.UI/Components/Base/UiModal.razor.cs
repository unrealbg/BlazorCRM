namespace Crm.UI.Components.Base
{
    using Microsoft.AspNetCore.Components;

    using System.Threading.Tasks;

    public partial class UiModal
    {
        [Parameter] 
        public bool Open { get; set; }

        [Parameter] 
        public EventCallback<bool> OpenChanged { get; set; }

        [Parameter] 
        public string? Title { get; set; }

        [Parameter] 
        public RenderFragment? ChildContent { get; set; }

        [Parameter] 
        public RenderFragment? Footer { get; set; }

        private async Task Close()
        {
            if (OpenChanged.HasDelegate)
            {
                await OpenChanged.InvokeAsync(false);
                return;
            }

            Open = false;
        }
    }
}