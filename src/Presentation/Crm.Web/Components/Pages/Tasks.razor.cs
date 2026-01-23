namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
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

                var list = await Service.GetAllAsync(_filter);

                if (!string.IsNullOrEmpty(_ownerFilter))
                {
                    list = list.Where(t => t.OwnerId?.ToString() == _ownerFilter);
                }

                if (!string.IsNullOrEmpty(_priority) && Enum.TryParse<TaskPriority>(_priority, out var pr))
                {
                    list = list.Where(t => t.Priority == pr);
                }

                if (!string.IsNullOrEmpty(_status) && Enum.TryParse<TaskStatusDomain>(_status, out var st))
                {
                    list = list.Where(t => t.Status == st);
                }

                _items = list.OrderBy(t => t.DueAt ?? DateTime.MaxValue).ToList();
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