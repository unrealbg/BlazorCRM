namespace Crm.Web.Components.Pages
{
    using Crm.Application.Services;
    using Crm.Domain.Entities;
    using Crm.Web.Components;

    using Microsoft.AspNetCore.Components;

    using System;
    using System.Threading.Tasks;

    using TaskStatusDomain = Crm.Domain.Enums.TaskStatus;

    public partial class TaskDetails
    {
        [Parameter] 
        public Guid Id { get; set; }

        [Inject] 
        ITaskService Service { get; set; } = default!;

        TaskItem? _task;
        bool _loading = true;
        string _status = TaskStatusDomain.Todo.ToString();
        DateTime? _dueLocal;
        Modal _editModal = default!;

        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            try
            {
                _task = await Service.GetByIdAsync(Id);
                _status = _task.Status.ToString();
                _dueLocal = _task.DueAt?.ToLocalTime();
            }
            catch
            {
                _task = null;
            }
            finally
            {
                _loading = false;
            }
        }

        private async Task SaveAsync()
        {
            if (_task is null)
            {
                return;
            }

            if (Enum.TryParse<TaskStatusDomain>(_status, out var st))
            {
                _task.Status = st;
            }

            _task.DueAt = _dueLocal is DateTime dt ? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime() : null;
            await Service.UpsertAsync(_task);
            _editModal.Hide();
        }
    }
}