namespace Crm.UI.Components.Base
{
    using Microsoft.AspNetCore.Components;

    using System.Collections.Generic;

    public partial class UiSkeleton
    {
        [Parameter] 
        public string? Class { get; set; }

        [Parameter] 
        public string? Style { get; set; }


        [Parameter(CaptureUnmatchedValues = true)] 
        public Dictionary<string, object>? AdditionalAttributes { get; set; }
    }
}