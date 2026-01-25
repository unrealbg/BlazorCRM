namespace Crm.Web.Components.Pages
{
    using Crm.Application.Companies.Queries;
    using Crm.Contracts.Paging;

    using MediatR;

    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    public partial class Companies
    {
        [Inject] 
        IMediator Mediator { get; set; } = default!;

        [Inject] 
        NavigationManager Nav { get; set; } = default!;

        [Inject] 
        IJSRuntime JS { get; set; } = default!;

        DotNetObjectReference<Companies>? _selfRef;
        List<CompanyListItem> _items = new();
        string? _search;
        string? _industry;
        HashSet<string> _industries = new();
        string _sort = nameof(Crm.Domain.Entities.Company.Name);
        bool _asc = true;
        int _page = 1;
        int _pageSize = 10;
        int _total = 0;
        int _pages = 1;
        string? _subKey;
        bool _subscribed;
        bool _loading;
        List<SavedView> _views = new();
        string? _selectedView;
        string? _newViewName;
        const string ViewsKey = "crm:companyViews";

        void ShowQuickCreate() => JS.InvokeVoidAsync("openModal", "modalQuickCreate");

        protected override async Task OnInitializedAsync()
        {
            await Reload();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !_subscribed)
            {
                _selfRef = DotNetObjectReference.Create(this);
                _subKey = await JS.InvokeAsync<string>("subscribe", "company:created", _selfRef, nameof(OnCompanyCreated));
                _subscribed = true;
            }

            if (firstRender)
            {
                await LoadViewsAsync();
            }
        }

        [JSInvokable]
        public async Task OnCompanyCreated(object? payload)
        {
            await Reload();
            StateHasChanged();
        }

        async Task Reload()
        {
            _loading = true;
            try
            {
                var res = await Mediator.Send(new SearchCompanies(new PagedRequest
                {
                    Search = _search,
                    Page = _page,
                    PageSize = _pageSize,
                    SortBy = _sort,
                    SortDir = _asc ? "asc" : "desc"
                }, _industry));
                _items = res.Items.ToList();
                _total = res.TotalCount;
                _pages = Math.Max(1, (int)Math.Ceiling(_total / (double)_pageSize));

                var list = await Mediator.Send(new DistinctIndustries(_search));
                _industries = list.ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            finally { _loading = false; }
        }

        async Task ApplyFilters()
        {
            _page = 1;
            await Reload();
        }

        async Task ClearFilters()
        {
            _search = null;
            _industry = null;
            _page = 1;
            await Reload();
        }

        async Task LoadViewsAsync()
        {
            try
            {
                var json = await JS.InvokeAsync<string>("localStorage.getItem", ViewsKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    _views = JsonSerializer.Deserialize<List<SavedView>>(json) ?? new();
                }
            }
            catch
            {
                _views = new();
            }

            if (_views.All(v => !string.Equals(v.Name, "All companies", StringComparison.OrdinalIgnoreCase)))
            {
                _views.Insert(0, new SavedView { Name = "All companies" });
            }
        }

        async Task SaveView()
        {
            if (string.IsNullOrWhiteSpace(_newViewName))
            {
                return;
            }

            var view = new SavedView
            {
                Name = _newViewName.Trim(),
                Search = _search,
                Industry = _industry,
                Sort = _sort,
                Asc = _asc,
                PageSize = _pageSize
            };

            var existing = _views.FirstOrDefault(v => string.Equals(v.Name, view.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                _views.Remove(existing);
            }
            _views.Add(view);
            _selectedView = view.Name;
            _newViewName = null;
            await PersistViewsAsync();
        }

        async Task PersistViewsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_views);
                await JS.InvokeVoidAsync("localStorage.setItem", ViewsKey, json);
            }
            catch
            {
                // ignore storage errors
            }
        }

        async Task OnViewChanged(ChangeEventArgs e)
        {
            _selectedView = e.Value?.ToString();
            var view = _views.FirstOrDefault(v => string.Equals(v.Name, _selectedView, StringComparison.OrdinalIgnoreCase));
            if (view is null)
            {
                return;
            }

            _search = view.Search;
            _industry = view.Industry;
            _sort = view.Sort ?? _sort;
            _asc = view.Asc;
            _pageSize = view.PageSize > 0 ? view.PageSize : _pageSize;
            _page = 1;
            await Reload();
        }

        void SortBy(string prop)
        {
            if (_sort == prop)
            {
                _asc = !_asc;
            }
            else
            {
                _sort = prop;
                _asc = true;
            }

            _page = 1; 
            _ = Reload();
        }

        void PrevPage()
        {
            _page = Math.Max(1, _page - 1); _ = Reload();
        }
        void NextPage()
        {
            _page = Math.Min(_pages, _page + 1); _ = Reload();
        }

        int StartRow => _total == 0 ? 0 : ((_page - 1) * _pageSize) + 1;
        int EndRow => Math.Min(_total, _page * _pageSize);

        RenderFragment SortGlyph(string p) => builder =>
        {
            if (_sort != p)
            {
                return;
            }

            builder.AddContent(0, _asc ? " ?" : " ?");
        };

        void View(Guid id) => Nav.NavigateTo($"/companies/{id}");
        void Edit(Guid id) => Nav.NavigateTo($"/companies/{id}?edit=1");

        private sealed class SavedView
        {
            public string Name { get; set; } = string.Empty;
            public string? Search { get; set; }
            public string? Industry { get; set; }
            public string? Sort { get; set; }
            public bool Asc { get; set; } = true;
            public int PageSize { get; set; } = 10;
        }

        public async ValueTask DisposeAsync()
        {
            if (_selfRef is not null)
            {
                _selfRef.Dispose();
            }

            if (!string.IsNullOrEmpty(_subKey))
            {
                await JS.InvokeVoidAsync("unsubscribe", _subKey);
            }
        }
    }
}