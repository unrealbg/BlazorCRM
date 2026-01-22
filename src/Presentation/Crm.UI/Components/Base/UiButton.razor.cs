namespace Crm.UI.Components.Base
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    using System.Collections.Generic;
    using System.Linq;

    public partial class UiButton
    {
        [Parameter]
        public string Type { get; set; } = "button";

        [Parameter]
        public string Variant { get; set; } = "primary";

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public string? Class { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object>? AdditionalAttributes { get; set; }

        private string VariantClass => Variant switch
        {
            "muted" => "ui-btn-muted",
            _ => "ui-btn-primary"
        };

        private string CssClass => string.Join(" ", new[] { "ui-btn", VariantClass, Class }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}