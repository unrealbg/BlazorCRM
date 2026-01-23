namespace Crm.UI.Components.Base
{
    using Microsoft.AspNetCore.Components;

    using System.Collections.Generic;

    public partial class UiTable
    {
        [Parameter] 
        public string? Class { get; set; }

        [Parameter] 
        public RenderFragment? ChildContent { get; set; }

        [Parameter(CaptureUnmatchedValues = true)] 
        public Dictionary<string, object>? AdditionalAttributes { get; set; }
    }
}