namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
    using Crm.Contracts.Paging;
    using Crm.Domain.Entities;
    using Crm.Domain.Enums;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Identity;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using TaskStatusDomain = Crm.Domain.Enums.TaskStatus;

    public partial class Tasks
    {
        [Inject] 
        ITaskService Service { get; set; } = default!;

        [Inject] 
        UserManager<IdentityUser> UserManager { get; set; } = default!;

        bool _loading = true;
        string? _filter;
        string? _ownerFilter;
        string? _priority;
        string? _status;
        List<TaskItem> _items = new();
        List<IdentityUser> _users = new();

        protected override async Task OnInitializedAsync() => await Reload();

        private async Task Reload()
        {
            _loading = true;
            try
            {
                _users = UserManager.Users.ToList();

                Guid? ownerId = null;
                if (!string.IsNullOrEmpty(_ownerFilter) && Guid.TryParse(_ownerFilter, out var parsedOwner))
                {
                    ownerId = parsedOwner;
                }

                TaskPriority? priority = null;
                if (!string.IsNullOrEmpty(_priority) && Enum.TryParse<TaskPriority>(_priority, out var pr))
                {
                    priority = pr;
                }

                TaskStatusDomain? status = null;
                if (!string.IsNullOrEmpty(_status) && Enum.TryParse<TaskStatusDomain>(_status, out var st))
                {
                    status = st;
                }

                var res = await Service.SearchAsync(new PagedRequest
                {
                    Search = _filter,
                    Page = 1,
                    PageSize = 200,
                    SortBy = nameof(TaskItem.DueAt),
                    SortDir = "asc"
                }, ownerId, priority, status, null, null);
                _items = res.Items.ToList();
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task ChangeStatus(TaskItem t, string? value)
        {
            if (!Enum.TryParse<TaskStatusDomain>(value, out var st))
            {
                return;
            }

            t.Status = st;
            await Service.UpsertAsync(t);
            await Reload();
            StateHasChanged();
        }

        private async Task ChangeOwner(TaskItem t, string? userId)
        {
            t.OwnerId = string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId);
            await Service.UpsertAsync(t);
            await Reload();
            StateHasChanged();
        }

        string OwnerName(Guid? ownerId)
        {
            if (ownerId is null)
            {
                return "Unassigned";
            }

            var id = ownerId.Value.ToString();
            var u = _users.FirstOrDefault(x => x.Id == id);
            return u is null ? id : DisplayName(u);
        }

        static string DisplayName(IdentityUser u)
          => string.IsNullOrWhiteSpace(u.Email) ? (u.UserName ?? u.Id) : u.Email!;
    }
}