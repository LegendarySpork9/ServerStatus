// Copyright © - 05/10/2025 - Toby Hunter
using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace ServerStatusSite.Components.Pages
{
    public partial class Error : ComponentBase
    {
        [CascadingParameter]
        private HttpContext? HttpContext { get; set; }

        private string? RequestId { get; set; }

        private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        protected override void OnInitialized() => RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
    }
}
