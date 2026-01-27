namespace Crm.Web.Components
{
    using Microsoft.AspNetCore.Components;
    using System.Threading.Tasks;

    public partial class SidebarNav
    {
        [Parameter]
        public EventCallback OnNavigate { get; set; }

        private async Task HandleNavigate()
        {
            if (OnNavigate.HasDelegate)
            {
                await OnNavigate.InvokeAsync();
            }
        }
    }
}
