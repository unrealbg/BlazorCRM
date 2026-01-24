namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
    using Crm.UI.Components;

    using Microsoft.AspNetCore.Components;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class Activities
    {
        [Inject] 
        IActivityService Service { get; set; } = default!;

        bool _loading = true;
        List<ActivityTimelineItem> _items = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            _loading = true;
            try
            {
                                var res = await Service.GetPageAsync(page: 1, pageSize: 50);
                                _items = res.Items
                                    .Select(a => new ActivityTimelineItem(
                    Icon: a.Type switch { Crm.Domain.Enums.ActivityType.Call => "??", Crm.Domain.Enums.ActivityType.Meeting => "??", Crm.Domain.Enums.ActivityType.Email => "??", _ => "??" },
                    Title: a.Notes ?? a.Type.ToString(),
                    Subtitle: a.RelatedTo switch { Crm.Domain.Enums.RelatedToType.Company when a.RelatedId is Guid cid => $"Company - {cid}", Crm.Domain.Enums.RelatedToType.Contact when a.RelatedId is Guid coid => $"Contact - {coid}", Crm.Domain.Enums.RelatedToType.Deal when a.RelatedId is Guid did => $"Deal - {did}", _ => null },
                    WhenUtc: a.DueAt,
                    Status: a.Status.ToString(),
                    LinkHref: a.RelatedTo switch { Crm.Domain.Enums.RelatedToType.Company when a.RelatedId is Guid cid => $"/companies/{cid}", Crm.Domain.Enums.RelatedToType.Contact when a.RelatedId is Guid coid => $"/contacts/{coid}", Crm.Domain.Enums.RelatedToType.Deal when a.RelatedId is Guid did => $"/deals/{did}", _ => null },
                    LinkLabel: a.RelatedTo.ToString()
                  ))
                  .ToList();
            }
            finally
            {
                _loading = false;
            }
        }
    }
}