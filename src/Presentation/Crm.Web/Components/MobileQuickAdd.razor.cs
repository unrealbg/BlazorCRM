namespace Crm.Web.Components
{
    using System;
    using System.Threading.Tasks;
    using Crm.Application.Activities;
    using Crm.Application.Tasks;
    using Crm.Domain.Enums;
    using MediatR;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;

    public partial class MobileQuickAdd : IDisposable
    {
        [Inject]
        IMediator Mediator { get; set; } = default!;

        [Inject]
        NavigationManager Nav { get; set; } = default!;

        [Inject]
        IJSRuntime JS { get; set; } = default!;

        bool _sheetOpen;
        bool _busy;
        QuickAddMode _mode = QuickAddMode.Activity;

        RelatedToType _relatedTo = RelatedToType.None;
        Guid? _relatedId;

        readonly ActivityForm _activity = new();
        readonly NoteForm _note = new();
        readonly TaskForm _task = new();

        protected override void OnInitialized()
        {
            UpdateContextFromUri(Nav.Uri);
            Nav.LocationChanged += OnLocationChanged;
        }

        void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            UpdateContextFromUri(e.Location);
            StateHasChanged();
        }

        void UpdateContextFromUri(string uri)
        {
            var path = new Uri(uri).AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(path))
            {
                _relatedTo = RelatedToType.None;
                _relatedId = null;
                return;
            }

            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && Guid.TryParse(parts[1], out var id))
            {
                _relatedId = id;
                _relatedTo = parts[0] switch
                {
                    "contacts" => RelatedToType.Contact,
                    "companies" => RelatedToType.Company,
                    "deals" => RelatedToType.Deal,
                    _ => RelatedToType.None
                };

                if (_relatedTo == RelatedToType.None)
                {
                    _relatedId = null;
                }

                return;
            }

            _relatedTo = RelatedToType.None;
            _relatedId = null;
        }

        void OpenSheet() => _sheetOpen = true;
        void CloseSheet() => _sheetOpen = false;

        void SetMode(QuickAddMode mode) => _mode = mode;

        async Task SaveActivityAsync()
        {
            if (_busy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_activity.Notes))
            {
                await JS.InvokeVoidAsync("showToast", "Notes are required", "error");
                return;
            }

            try
            {
                _busy = true;
                DateTime? dueUtc = _activity.DueAt.HasValue
                    ? DateTime.SpecifyKind(_activity.DueAt.Value, DateTimeKind.Local).ToUniversalTime()
                    : null;

                await Mediator.Send(new CreateActivity(
                    _activity.Type,
                    _relatedTo,
                    _relatedId,
                    dueUtc,
                    ActivityStatus.Pending,
                    _activity.Notes?.Trim()));

                await JS.InvokeVoidAsync("showToast", "Activity added.");
                _activity.Reset();
                CloseSheet();
            }
            catch (Exception ex)
            {
                await JS.InvokeVoidAsync("showToast", ex.Message, "error");
            }
            finally
            {
                _busy = false;
            }
        }

        async Task SaveNoteAsync()
        {
            if (_busy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_note.Notes))
            {
                await JS.InvokeVoidAsync("showToast", "Note is required", "error");
                return;
            }

            try
            {
                _busy = true;
                await Mediator.Send(new CreateActivity(
                    ActivityType.Note,
                    _relatedTo,
                    _relatedId,
                    null,
                    ActivityStatus.Pending,
                    _note.Notes?.Trim()));

                await JS.InvokeVoidAsync("showToast", "Note added.");
                _note.Reset();
                CloseSheet();
            }
            catch (Exception ex)
            {
                await JS.InvokeVoidAsync("showToast", ex.Message, "error");
            }
            finally
            {
                _busy = false;
            }
        }

        async Task SaveTaskAsync()
        {
            if (_busy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_task.Title))
            {
                await JS.InvokeVoidAsync("showToast", "Title is required", "error");
                return;
            }

            try
            {
                _busy = true;
                DateTime? dueUtc = _task.DueAt.HasValue
                    ? DateTime.SpecifyKind(_task.DueAt.Value, DateTimeKind.Local).ToUniversalTime()
                    : null;

                await Mediator.Send(new CreateTask(
                    _task.Title.Trim(),
                    dueUtc,
                    null,
                    _relatedTo,
                    _relatedId,
                    TaskPriority.Medium,
                    Crm.Domain.Enums.TaskStatus.Todo));

                await JS.InvokeVoidAsync("showToast", "Task added.");
                _task.Reset();
                CloseSheet();
            }
            catch (Exception ex)
            {
                await JS.InvokeVoidAsync("showToast", ex.Message, "error");
            }
            finally
            {
                _busy = false;
            }
        }

        public void Dispose()
        {
            Nav.LocationChanged -= OnLocationChanged;
        }

        enum QuickAddMode
        {
            Activity,
            Note,
            Task
        }

        sealed class ActivityForm
        {
            public ActivityType Type { get; set; } = ActivityType.Call;
            public string? Notes { get; set; }
            public DateTime? DueAt { get; set; }

            public void Reset()
            {
                Type = ActivityType.Call;
                Notes = null;
                DueAt = null;
            }
        }

        sealed class NoteForm
        {
            public string? Notes { get; set; }

            public void Reset() => Notes = null;
        }

        sealed class TaskForm
        {
            public string Title { get; set; } = string.Empty;
            public DateTime? DueAt { get; set; }

            public void Reset()
            {
                Title = string.Empty;
                DueAt = null;
            }
        }
    }
}