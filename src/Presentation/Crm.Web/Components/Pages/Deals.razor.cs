namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;
    using Crm.Domain.Enums;
    using Crm.UI.Components;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

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

        [Inject]
        ICompanyService CompanyService { get; set; } = default!;

        [Inject]
        IContactService ContactService { get; set; } = default!;

        [Inject]
        UserManager<IdentityUser> UserManager { get; set; } = default!;

        [Inject]
        IServiceScopeFactory ScopeFactory { get; set; } = default!;

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
        Dictionary<Guid, string> _companyNames = new();
        Dictionary<Guid, string> _contactNames = new();
        List<IdentityUser> _users = new();
        HashSet<Guid> _expandedDealIds = new();
        Deal? _moveDeal;
        bool _showMoveSheet;
        readonly CancellationTokenSource _disposeCts = new();
        bool _disposed;

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
                var ct = _disposeCts.Token;
                _stages = _selectedPipelineId == Guid.Empty
                    ? new()
                    : (await PipelineService.GetStagesAsync(_selectedPipelineId, ct)).OrderBy(s => s.Order).ToList();

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
                                        }, pipelineId: _selectedPipelineId, ct: ct)).Items;

                                if (ct.IsCancellationRequested) return;

                                _users = await LoadUsersAsync(ct);
                                if (ct.IsCancellationRequested) return;
                                await LoadCompanyNamesAsync(deals, ct);
                                await LoadContactNamesAsync(deals, ct);

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
            _editDeal = await DealService.GetByIdAsync(deal.Id, _disposeCts.Token);
            _drawerOpen = true;
            await LoadTimelineAsync(deal.Id);
        }

        async Task LoadTimelineAsync(Guid dealId)
        {
            _loadingTimeline = true;
            try
            {
                                var ct = _disposeCts.Token;
                                var activities = await ActivityService.GetPageAsync(new PagedRequest
                                {
                                    Page = 1,
                                    PageSize = 50,
                                    SortBy = nameof(Activity.DueAt),
                                    SortDir = "desc"
                                }, dealId, ct: ct);
                                if (ct.IsCancellationRequested) return;
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

            await DealService.UpsertAsync(_editDeal, _disposeCts.Token);
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

            var ok = await DealService.MoveToStageAsync(dealId, targetStageId, _disposeCts.Token);
            if (!ok)
            {
                await Reload();
            }
        }

        void ToggleDealDetails(Guid dealId)
        {
            if (!_expandedDealIds.Add(dealId))
            {
                _expandedDealIds.Remove(dealId);
            }
        }

        bool IsDealExpanded(Guid dealId) => _expandedDealIds.Contains(dealId);

        void OpenMoveSheet(Deal deal)
        {
            _moveDeal = deal;
            _showMoveSheet = true;
        }

        void CloseMoveSheet()
        {
            _showMoveSheet = false;
            _moveDeal = null;
        }

        async Task MoveDealToStageMobileAsync(Deal deal, Guid targetStageId)
        {
            if (deal.StageId == targetStageId)
            {
                CloseMoveSheet();
                return;
            }

            if (_dealsByStage.TryGetValue(deal.StageId, out var from) && _dealsByStage.TryGetValue(targetStageId, out var to))
            {
                from.Remove(deal);
                deal.StageId = targetStageId;
                to.Insert(0, deal);
                StateHasChanged();
            }

            CloseMoveSheet();

            var ok = await DealService.MoveToStageAsync(deal.Id, targetStageId, _disposeCts.Token);
            if (!ok)
            {
                await Reload();
            }
        }

        async Task LoadCompanyNamesAsync(IEnumerable<Deal> deals, CancellationToken ct)
        {
            var ids = deals
                .Where(d => d.CompanyId.HasValue)
                .Select(d => d.CompanyId!.Value)
                .Distinct()
                .Where(id => !_companyNames.ContainsKey(id))
                .ToList();

            foreach (var id in ids)
            {
                try
                {
                    var company = await CompanyService.GetByIdAsync(id, ct);
                    _companyNames[id] = company.Name;
                }
                catch
                {
                    // ignore missing company
                }
            }
        }

        async Task LoadContactNamesAsync(IEnumerable<Deal> deals, CancellationToken ct)
        {
            var ids = deals
                .Where(d => d.ContactId.HasValue)
                .Select(d => d.ContactId!.Value)
                .Distinct()
                .Where(id => !_contactNames.ContainsKey(id))
                .ToList();

            foreach (var id in ids)
            {
                try
                {
                    var contact = await ContactService.GetByIdAsync(id, ct);
                    _contactNames[id] = ContactDisplayName(contact);
                }
                catch
                {
                    // ignore missing contact
                }
            }
        }

        string DealPartyLabel(Deal d)
        {
            var parts = new List<string>();

            if (d.CompanyId.HasValue)
            {
                parts.Add(CompanyName(d.CompanyId));
            }

            if (d.ContactId.HasValue)
            {
                parts.Add(ContactName(d.ContactId));
            }

            return parts.Count == 0 ? "No company/contact" : string.Join(" / ", parts);
        }

        string CompanyName(Guid? companyId)
        {
            if (companyId is null)
            {
                return "No company";
            }

            return _companyNames.TryGetValue(companyId.Value, out var name)
                ? name
                : companyId.Value.ToString();
        }

        string ContactName(Guid? contactId)
        {
            if (contactId is null)
            {
                return "No contact";
            }

            return _contactNames.TryGetValue(contactId.Value, out var name)
                ? name
                : contactId.Value.ToString();
        }

        string OwnerName(Guid? ownerId)
        {
            if (ownerId is null)
            {
                return "Unassigned";
            }

            var id = ownerId.Value.ToString();
            var user = _users.FirstOrDefault(u => u.Id == id);
            return user is null ? id : DisplayName(user);
        }

        static string DisplayName(IdentityUser u)
          => string.IsNullOrWhiteSpace(u.Email) ? (u.UserName ?? u.Id) : u.Email!;

        static string ContactDisplayName(Contact c) => $"{c.FirstName} {c.LastName}";

                async Task<List<IdentityUser>> LoadUsersAsync(CancellationToken ct)
                {
                        using var scope = ScopeFactory.CreateScope();
                        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                    return await manager.Users.ToListAsync(ct);
                }

                public void Dispose()
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;
                    _disposeCts.Cancel();
                    _disposeCts.Dispose();
                }
    }
}