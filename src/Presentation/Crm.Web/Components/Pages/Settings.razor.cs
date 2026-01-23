namespace Crm.Web.Components.Pages
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using System.Threading.Tasks;

    public partial class Settings
    {
        [Inject] 
        IJSRuntime JS { get; set; } = default!;

        [Inject] 
        Crm.Web.Services.ThemeState Theme { get; set; } = default!;

        async Task ToggleTheme()
        {
            var next = !Theme.IsDark;
            Theme.Set(next);
            await JS.InvokeVoidAsync("setTheme", next, true);
        }
    }
}