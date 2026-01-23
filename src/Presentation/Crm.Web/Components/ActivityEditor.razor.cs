namespace Crm.Web.Components
{
    using Crm.Application.Services;
    using Crm.Domain.Enums;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.JSInterop;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class ActivityEditor
    {
        [Parameter] 
        public RelatedToType RelatedTo { get; set; }

        [Parameter] 
        public Guid RelatedId { get; set; }

        [Parameter] 
        public EventCallback OnSaved { get; set; }

        [Inject] 
        IActivityService Service { get; set; } = default!;

        [Inject] 
        UserManager<IdentityUser> UserManager { get; set; } = default!;

        Modal _modal = default!;
        ConfirmModal _confirm = default!;
        Guid _id;
        string _type = ActivityType.Note.ToString();
        string _status = ActivityStatus.Pending.ToString();
        string? _notes;
        DateTime? _dueLocal;
        string? _owner;
        List<IdentityUser> _users = new();

        public async void Show(Guid? id)
        {
            _users = UserManager.Users.ToList();
            _id = id ?? Guid.Empty;

            if (_id == Guid.Empty)
            {
                _type = ActivityType.Note.ToString();
                _status = ActivityStatus.Pending.ToString();
                _notes = null;
                _owner = null;
                _dueLocal = null;
            }
            else
            {
                var a = await Service.GetByIdAsync(_id);
                if (a is not null)
                {
                    _type = a.Type.ToString();
                    _status = a.Status.ToString();
                    _notes = a.Notes;
                    _owner = a.OwnerId?.ToString();
                    _dueLocal = a.DueAt?.ToLocalTime();
                }
            }

            _modal.Show();
            StateHasChanged();
        }

        private async Task SaveAsync()
        {
            if (!Enum.TryParse<ActivityType>(_type, out var t))
            {
                t = ActivityType.Note;
            }

            if (!Enum.TryParse<ActivityStatus>(_status, out var st))
            {
                st = ActivityStatus.Pending;
            }

            if (_id == Guid.Empty)
            {
                var a = new Crm.Domain.Entities.Activity
                {
                    Type = t,
                    RelatedTo = RelatedTo,
                    RelatedId = RelatedId,
                    DueAt = _dueLocal is DateTime dt ? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime() : null,
                    Status = st,
                    Notes = _notes,
                    OwnerId = string.IsNullOrEmpty(_owner) ? null : Guid.Parse(_owner)
                };
                await Service.UpsertAsync(a);
            }
            else
            {
                var existing = await Service.GetByIdAsync(_id);
                existing.Type = t;
                existing.Status = st;
                existing.Notes = _notes;
                existing.DueAt = _dueLocal is DateTime dt ? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime() : null;
                existing.OwnerId = string.IsNullOrEmpty(_owner) ? null : Guid.Parse(_owner);
                await Service.UpsertAsync(existing);
            }

            _modal.Hide();
            if (OnSaved.HasDelegate)
            {
                await OnSaved.InvokeAsync();
            }
        }

        private void AskDelete()
        {
            if (_id == Guid.Empty)
            {
                return;
            }

            _confirm.Show();
        }

        private async Task ConfirmDeleteAsync()
        {
            if (_id == Guid.Empty)
            {
                return;
            }

            await Service.DeleteAsync(_id);
            _modal.Hide();
            if (OnSaved.HasDelegate)
            {
                await OnSaved.InvokeAsync();
            }
        }

        static string DisplayName(IdentityUser u)
          => string.IsNullOrWhiteSpace(u.Email) ? (u.UserName ?? u.Id) : u.Email!;
    }
}