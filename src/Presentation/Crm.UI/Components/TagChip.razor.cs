namespace Crm.UI.Components
{
    using Microsoft.AspNetCore.Components;

    public partial class TagChip
    {
        [Parameter] 
        public string Tag { get; set; } = string.Empty;

        [Parameter] 
        public bool Removable { get; set; } = true;

        [Parameter] 
        public EventCallback<string> OnRemove { get; set; }
    }
}