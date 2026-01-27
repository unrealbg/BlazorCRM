namespace Crm.Web.Components
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using System;
    using System.Threading.Tasks;

    public partial class AppShell : IDisposable
    {
        [Inject] 
        IJSRuntime JS { get; set; } = default!;

        [Inject] 
        Crm.Web.Services.ThemeState Theme { get; set; } = default!;

        [Inject]
        Crm.Web.Services.MobileNavState MobileNav { get; set; } = default!;

        bool SidebarCollapsed { get; set; }
        private DotNetObjectReference<AppShell>? _dotNetRef;

        protected override void OnInitialized()
        {
            Theme.OnChange += OnThemeChanged;
            MobileNav.OnChange += OnMobileNavChanged;
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

        void ToggleMobileNav()
        {
            MobileNav.Toggle();
        }

        void CloseMobileNav()
        {
            MobileNav.Close();
        }

        [JSInvokable]
        public void HandleEscapeKey()
        {
            CloseMobileNav();
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

                _dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("addGlobalEscapeListener", _dotNetRef);
            }
            catch (InvalidOperationException)
            {
                // JS interop not available during prerender; will run on interactive render.
            }
        }

        void OnThemeChanged() => InvokeAsync(StateHasChanged);
        void OnMobileNavChanged() => InvokeAsync(StateHasChanged);

        public void Dispose()
        {
            Theme.OnChange -= OnThemeChanged;
            MobileNav.OnChange -= OnMobileNavChanged;
            
            if (_dotNetRef is not null)
            {
                try
                {
                    JS.InvokeVoidAsync("removeGlobalEscapeListener");
                }
                catch
                {
                    // Ignore disposal errors
                }
                _dotNetRef.Dispose();
            }
        }
    }
}