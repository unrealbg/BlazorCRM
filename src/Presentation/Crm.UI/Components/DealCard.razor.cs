namespace Crm.UI.Components
{
    using Microsoft.AspNetCore.Components;

    public partial class DealCard
    {
        [Parameter] 
        public string Title { get; set; } = string.Empty;

        [Parameter] 
        public decimal? Amount { get; set; }
    }
}