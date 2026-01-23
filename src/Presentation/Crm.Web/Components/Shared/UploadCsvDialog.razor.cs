namespace Crm.Web.Components.Shared
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Forms;
    using Microsoft.JSInterop;

    using System.Threading.Tasks;

    public partial class UploadCsvDialog
    {
        [Parameter] 
        public EventCallback<Stream> OnUploaded { get; set; }

        [Parameter] 
        public EventCallback OnClose { get; set; }

        private async Task OnFileChange(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file is null)
            {
                return;
            }

            await using var stream = file.OpenReadStream(long.MaxValue);
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            await OnUploaded.InvokeAsync(ms);
            await OnClose.InvokeAsync();
        }

        private Task Cancel() => OnClose.InvokeAsync();
    }
}