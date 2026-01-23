namespace Crm.Web.Components
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using System;
    using System.Threading.Tasks;

    public partial class HeaderBar
    {
        [Inject] 
        IJSRuntime JS { get; set; } = default!;

        [CascadingParameter] 
        IServiceProvider Services { get; set; } = default!;

        Task ToggleSidebar() => JS.InvokeVoidAsync("toggleSidebar").AsTask();
        Task OpenPalette() => JS.InvokeVoidAsync("openPalette").AsTask();

        async Task OpenQuickCreate()
        {
            await JS.InvokeVoidAsync("openModal", "modalQuickCreate");
        }
    }
}