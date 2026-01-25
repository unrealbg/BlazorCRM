namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;
    using Crm.Domain.Enums;
    using Crm.UI.Components;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class Deals
    {
        [Inject] 
        IPipelineService PipelineService { get; set; } = default!;

        [Inject] 
        IDealService DealService { get; set; } = default!;

        [Inject] 
        IActivityService ActivityService { get; set; } = default!;

        bool _loading = true;
        List<Pipeline> _pipelines = new();
        List<Stage> _stages = new();
        Guid _selectedPipelineId;
        Dictionary<Guid, List<Deal>> _dealsByStage = new();
        Guid? _draggingDealId;
        Guid? _ghostDealId;
        Guid? _hoveredStageId;
        bool _drawerOpen;
        Deal? _drawerDeal;
        Deal? _editDeal;
        List<ActivityTimelineItem> _timeline = new();
        bool _loadingTimeline;
        Dictionary<Guid, string> _stageNameMap = new();

        Guid SelectedPipelineId
        {
            get => _selectedPipelineId;
            set
            {
                if (_selectedPipelineId != value)
                {
                    _selectedPipelineId = value;
                    _ = Reload();
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadPipelines();
            if (_selectedPipelineId != Guid.Empty)
            {
                await Reload();
            }
        }

        private async Task LoadPipelines()
        {
            _pipelines = (await PipelineService.GetPipelinesAsync()).ToList();
            _selectedPipelineId = _pipelines.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        private async Task Reload()
        {
            _loading = true;
            try
            {
                _stages = _selectedPipelineId == Guid.Empty
                  ? new()
                  : (await PipelineService.GetStagesAsync(_selectedPipelineId)).OrderBy(s => s.Order).ToList();

                _stageNameMap = _stages.ToDictionary(s => s.Id, s => s.Name);

                _dealsByStage.Clear();
                var deals = _selectedPipelineId == Guid.Empty
                    ? Enumerable.Empty<Deal>()
                    : (await DealService.GetPageAsync(new PagedRequest
                    {
                        Page = 1,
                        PageSize = 200,
                        SortBy = nameof(Deal.Amount),
                        SortDir = "desc"
                    }, pipelineId: _selectedPipelineId)).Items;

                foreach (var g in deals.GroupBy(d => d.StageId))
                {
                    _dealsByStage[g.Key] = g.ToList();
                }

                foreach (var s in _stages)
                {
                    if (!_dealsByStage.ContainsKey(s.Id)) _dealsByStage[s.Id] = new();
                }
            }
            finally
            {
                _loading = false;
            }
        }

        async Task OpenDrawer(Deal deal)
        {
            _drawerDeal = deal;
            _editDeal = await DealService.GetByIdAsync(deal.Id);
            _drawerOpen = true;
            await LoadTimelineAsync(deal.Id);
        }

        async Task LoadTimelineAsync(Guid dealId)
        {
            _loadingTimeline = true;
            try
            {
                                var activities = await ActivityService.GetPageAsync(new PagedRequest
                                {
                                    Page = 1,
                                    PageSize = 50,
                                    SortBy = nameof(Activity.DueAt),
                                    SortDir = "desc"
                                }, dealId);
                                _timeline = activities.Items
                                    .Select(a => new ActivityTimelineItem(
                    a.Type switch
                    {
                        ActivityType.Call => "Call",
                        ActivityType.Meeting => "Meeting",
                        ActivityType.Email => "Email",
                        _ => "Note"
                    },
                    a.Type.ToString(),
                    a.Notes,
                    a.DueAt,
                    a.Status.ToString()))
                  .ToList();
            }
            finally
            {
                _loadingTimeline = false;
            }
        }

        async Task SaveQuickEditAsync()
        {
            if (_editDeal is null)
            {
                return;
            }

            await DealService.UpsertAsync(_editDeal);
            _drawerOpen = false;
            await Reload();
        }

        void CloseDrawer() => _drawerOpen = false;

        void OnDragStart(Deal d)
        {
            _draggingDealId = d.Id;
            _ghostDealId = d.Id;
        }

        void OnDragOver(DragEventArgs _)
        {
            // handled in child via preventDefault
        }

        void OnDragEnter(Guid stageId) => _hoveredStageId = stageId;
        void OnDragLeave(Guid stageId) { if (_hoveredStageId == stageId) _hoveredStageId = null; }

        async Task OnDropAsync(Guid targetStageId)
        {
            var dealId = _draggingDealId ?? Guid.Empty;
            if (dealId == Guid.Empty)
            {
                return;
            }

            var originStage = _dealsByStage.FirstOrDefault(kv => kv.Value.Any(d => d.Id == dealId)).Key;
            if (originStage != Guid.Empty && originStage != targetStageId && _dealsByStage.TryGetValue(originStage, out var from) && _dealsByStage.TryGetValue(targetStageId, out var to))
            {
                var deal = from.First(d => d.Id == dealId);
                from.Remove(deal);
                deal.StageId = targetStageId;
                to.Insert(0, deal);
                StateHasChanged();
            }

            _hoveredStageId = null;
            _ghostDealId = null;

            var ok = await DealService.MoveToStageAsync(dealId, targetStageId);
            if (!ok)
            {
                await Reload();
            }
        }
    }
}