namespace Crm.UI.Components
{
    using Microsoft.AspNetCore.Components;

    using System.Collections.Generic;

    public partial class ActivityTimeline
    {
        [Parameter] 
        public IEnumerable<ActivityTimelineItem>? Items { get; set; }


        [Parameter] 
        public RenderFragment<ActivityTimelineItem>? ItemTemplate { get; set; }
    }
}