namespace Crm.Web.Components
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using System;
    using System.Threading.Tasks;

    public partial class App
    {
        [Inject] 
        IJSRuntime JS { get; set; } = default!;

        [Inject] 
        Crm.Web.Services.ThemeState Theme { get; set; } = default!;

        bool SidebarCollapsed { get; set; }

        protected override void OnInitialized()
        {
            Theme.OnChange += OnThemeChanged;
        }

        async Task ToggleTheme()
        {
            var next = !Theme.IsDark;
            Theme.Set(next);
            await JS.InvokeVoidAsync("setTheme", next, true);
        }

        void ToggleSidebarCollapse()
        {
            SidebarCollapsed = !SidebarCollapsed;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
            try
            {
                var initial = await JS.InvokeAsync<string>("getThemePreference");
                var isDark = string.Equals(initial, "dark", StringComparison.OrdinalIgnoreCase);
                Theme.Set(isDark);
                await JS.InvokeVoidAsync("setTheme", isDark, false);
            }
            catch (InvalidOperationException)
            {
                // JS interop not available during prerender; will run on interactive render.
            }
        }

        void OnThemeChanged() => InvokeAsync(StateHasChanged);

        public void Dispose()
        {
            Theme.OnChange -= OnThemeChanged;
        }
    }
}