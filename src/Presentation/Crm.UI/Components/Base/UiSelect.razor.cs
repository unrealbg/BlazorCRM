namespace Crm.UI.Components.Base
{
    using Microsoft.AspNetCore.Components;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public partial class UiSelect
    {
        [Parameter] 
        public string? Value { get; set; }

        [Parameter] 
        public EventCallback<string?> ValueChanged { get; set; }

        [Parameter] 
        public Expression<Func<string?>>? ValueExpression { get; set; }

        [Parameter] 
        public string? Id { get; set; }

        [Parameter] 
        public bool Disabled { get; set; }

        [Parameter] 
        public string? Class { get; set; }

        [Parameter] 
        public RenderFragment? ChildContent { get; set; }

        [Parameter(CaptureUnmatchedValues = true)] 
        public Dictionary<string, object>? AdditionalAttributes { get; set; }

        private string CssClass => string.Join(" ", new[] { "ui-select", Class }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}