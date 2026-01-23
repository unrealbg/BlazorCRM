namespace Crm.UI.Components
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using System.Collections.Generic;

    public partial class StageColumn
    {
        [Parameter] 
        public string Title { get; set; } = string.Empty;

        [Parameter] 
        public int Count { get; set; }

        [Parameter] 
        public RenderFragment? ChildContent { get; set; }

        [Parameter] 
        public string? Class { get; set; }

        [Parameter(CaptureUnmatchedValues = true)] 
        public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        [Parameter] 
        public EventCallback<DragEventArgs> OnDrop { get; set; }

        [Parameter] 
        public EventCallback<DragEventArgs> OnDragOver { get; set; }

        [Parameter] 
        public EventCallback<DragEventArgs> OnDragEnter { get; set; }

        [Parameter] 
        public EventCallback<DragEventArgs> OnDragLeave { get; set; }
    }
}