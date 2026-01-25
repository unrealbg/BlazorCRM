namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;
    using Crm.UI.Components;

    using Microsoft.AspNetCore.Components;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class Contacts
    {
        [Inject]
        IContactService ContactService { get; set; } = default!;

        [Inject]
        ICompanyService CompanyService { get; set; } = default!;

        List<Contact> _items = new();
        List<Contact> _mobileItems = new();
        Dictionary<Guid, string> _companyNames = new();
        string? _search;
        string _sort = nameof(Contact.LastName);
        bool _asc = true;
        int _page = 1;
        int _mobilePage = 1;
        int _pageSize = 10;
        int _total = 0;
        int _pages = 1;
        bool _loading;

        protected override async Task OnInitializedAsync()
        {
            await Reload();
        }

        async Task Reload()
        {
            _page = 1;
            _mobilePage = 1;
            await LoadTablePageAsync(resetMobile: true);
        }

        async Task LoadTablePageAsync(bool resetMobile)
        {
            _loading = true;
            try
            {
                var res = await ContactService.SearchAsync(new PagedRequest
                {
                    Search = _search,
                    Page = _page,
                    PageSize = _pageSize,
                    SortBy = _sort,
                    SortDir = _asc ? "asc" : "desc"
                });

                _items = res.Items.ToList();
                if (resetMobile)
                {
                    _mobileItems = res.Items.ToList();
                }

                _total = res.TotalCount;
                _pages = Math.Max(1, (int)Math.Ceiling(_total / (double)_pageSize));

                await LoadCompanyNamesAsync(res.Items);
            }
            finally
            {
                _loading = false;
            }
        }

        async Task LoadMoreMobile()
        {
            if (_mobilePage >= _pages)
            {
                return;
            }

            _loading = true;
            try
            {
                _mobilePage++;
                var res = await ContactService.SearchAsync(new PagedRequest
                {
                    Search = _search,
                    Page = _mobilePage,
                    PageSize = _pageSize,
                    SortBy = _sort,
                    SortDir = _asc ? "asc" : "desc"
                });

                _mobileItems.AddRange(res.Items);
                _total = res.TotalCount;
                _pages = Math.Max(1, (int)Math.Ceiling(_total / (double)_pageSize));

                await LoadCompanyNamesAsync(res.Items);
            }
            finally
            {
                _loading = false;
            }
        }

        async Task LoadCompanyNamesAsync(IEnumerable<Contact> contacts)
        {
            var ids = contacts
                .Where(c => c.CompanyId.HasValue)
                .Select(c => c.CompanyId!.Value)
                .Distinct()
                .Where(id => !_companyNames.ContainsKey(id))
                .ToList();

            foreach (var id in ids)
            {
                try
                {
                    var company = await CompanyService.GetByIdAsync(id);
                    _companyNames[id] = company.Name;
                }
                catch
                {
                    // ignore missing company
                }
            }
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
            _mobilePage = 1;
            _ = LoadTablePageAsync(resetMobile: true);
        }

        void PrevPage()
        {
            _page = Math.Max(1, _page - 1);
            _ = LoadTablePageAsync(resetMobile: false);
        }

        void NextPage()
        {
            _page = Math.Min(_pages, _page + 1);
            _ = LoadTablePageAsync(resetMobile: false);
        }

        int StartRow => _total == 0 ? 0 : ((_page - 1) * _pageSize) + 1;
        int EndRow => Math.Min(_total, _page * _pageSize);

        string CompanyLabel(Guid? companyId)
        {
            if (companyId is null)
            {
                return "—";
            }

            return _companyNames.TryGetValue(companyId.Value, out var name)
                ? name
                : companyId.Value.ToString();
        }

        static string DisplayName(Contact c) => $"{c.FirstName} {c.LastName}";

        RenderFragment SortGlyph(string p) => builder =>
        {
            if (_sort != p)
            {
                return;
            }

            builder.AddContent(0, _asc ? " ?" : " ?");
        };
    }
}
