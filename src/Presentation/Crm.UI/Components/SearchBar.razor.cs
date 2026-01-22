namespace Crm.UI.Components
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using System.Threading.Tasks;

    public partial class SearchBar
    {
        [Parameter] 
        public string? Value { get; set; }

        [Parameter] 
        public EventCallback<string?> ValueChanged { get; set; }

        [Parameter] 
        public string Placeholder { get; set; } = "Search";

        [Parameter] 
        public EventCallback<string?> OnSearch { get; set; }

        private async Task OnKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await OnSearch.InvokeAsync(Value);
            }
        }
    }
}