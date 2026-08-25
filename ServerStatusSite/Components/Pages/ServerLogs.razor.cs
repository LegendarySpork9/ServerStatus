// Copyright © - Unpublished - Toby Hunter
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Services;
using ServerStatusSite.Converters;
using ServerStatusSite.Models;
using ServerStatusSite.Models.Responses;
using ServerStatusSite.Models.Responses.Related;
using ServerStatusSite.Services;

namespace ServerStatusSite.Components.Pages
{
    public partial class ServerLogs : ComponentBase, IAsyncDisposable
    {
        [Inject]
        private ILoggerService _Logger { get; set; } = default!;
        [Inject]
        private IJSRuntime JS { get; set; } = default!;
        [Inject]
        private APIService APIService { get; set; } = default!;
        [Inject]
        private BackupToolAPIService BackupToolApi { get; set; } = default!;
        [Inject]
        private LogStreamService LogStream { get; set; } = default!;
        [Inject]
        private BackupToolSettingsModel BackupToolSettings { get; set; } = default!;
        [Inject]
        private UserModel User { get; set; } = default!;

        private List<ServerModel>? Servers;
        private List<string> ServerNames = [];
        private List<ArchivedLogFileModel> ArchiveFiles = [];
        private List<LogEntryModel> LogEntries = [];

        private bool IsLoading;
        private bool IsFetchingLogs;
        private bool IsFetchingOlder;
        private bool IsWebhookActive;
        private bool ScrollDetectionInitialised;

        private string SelectedServer = string.Empty;
        private string SelectedLogSource = string.Empty;
        private string SelectedLogType = "All";
        private string SelectedArchiveFile = string.Empty;
        private string? WebhookRegistrationId;
        private string ErrorMessage = string.Empty;
        private int? NextAfterCursor;
        private ElementReference ConsoleRef;
        private IJSObjectReference? JsModule;
        private DotNetObjectReference<ServerLogs>? DotNetRef;
        private readonly SemaphoreSlim LogLock = new(1, 1);

        /// <summary>
        /// Loads the active servers from the API.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Opened Server Logs Page");

            IsLoading = true;

            Servers = await APIService.GetServers();

            if (Servers != null)
            {
                ServerNames.AddRange(Servers.Where(s => s.IsActive)
                    .Select(s => s.Name));
            }

            IsLoading = false;
        }

        /// <summary>
        /// Initialises the JavaScript scroll detection module.
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!ScrollDetectionInitialised && LogEntries.Count > 0)
            {
                try
                {
                    JsModule = await JS.InvokeAsync<IJSObjectReference>(
                        "import",
                        "./js/logConsole.js");

                    DotNetRef = DotNetObjectReference.Create(this);

                    await JsModule.InvokeVoidAsync(
                        "initScrollDetection",
                        ConsoleRef,
                        DotNetRef);

                    await JsModule.InvokeVoidAsync(
                        "scrollToBottom",
                        ConsoleRef);

                    ScrollDetectionInitialised = true;
                }

                catch (Exception ex)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        $"Failed to initialise scroll detection: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Returns the CSS to change the page to dark mode.
        /// </summary>
        private string GetStyle(string? component = null)
        {
            return component switch
            {
                "Input" => StyleConverter.GetInputDarkMode(User.DarkMode),
                "StatusBar" => StyleConverter.GetTableDarkMode(User.DarkMode),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Handles the server dropdown change.
        /// </summary>
        private async Task ServerChanged(ChangeEventArgs e)
        {
            await CleanupWebhook();

            SelectedServer = e.Value?.ToString() ?? string.Empty;
            SelectedLogSource = string.Empty;
            SelectedLogType = "All";
            SelectedArchiveFile = string.Empty;
            ArchiveFiles = [];
            LogEntries = [];
            NextAfterCursor = null;
            ErrorMessage = string.Empty;
            ScrollDetectionInitialised = false;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Selected Server: {SelectedServer}");
        }

        /// <summary>
        /// Handles the log source dropdown change.
        /// </summary>
        private async Task LogSourceChanged(ChangeEventArgs e)
        {
            await CleanupWebhook();

            SelectedLogSource = e.Value?.ToString() ?? string.Empty;
            SelectedLogType = "All";
            SelectedArchiveFile = string.Empty;
            ArchiveFiles = [];
            LogEntries = [];
            NextAfterCursor = null;
            ErrorMessage = string.Empty;
            ScrollDetectionInitialised = false;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Selected Log Source: {SelectedLogSource}");

            if (SelectedLogSource == "Live")
            {
                await FetchInitialLogs();
            }

            else if (SelectedLogSource == "Archived")
            {
                await FetchArchiveList();
            }
        }

        /// <summary>
        /// Handles the log type dropdown change.
        /// </summary>
        private async Task LogTypeChanged(ChangeEventArgs e)
        {
            await CleanupWebhook();

            SelectedLogType = e.Value?.ToString() ?? "All";
            LogEntries = [];
            NextAfterCursor = null;
            ErrorMessage = string.Empty;
            ScrollDetectionInitialised = false;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Selected Log Type: {SelectedLogType}");

            await FetchInitialLogs();
        }

        /// <summary>
        /// Handles the archive file dropdown change.
        /// </summary>
        private async Task ArchiveFileChanged(ChangeEventArgs e)
        {
            SelectedArchiveFile = e.Value?.ToString() ?? string.Empty;
            LogEntries = [];
            NextAfterCursor = null;
            ErrorMessage = string.Empty;
            ScrollDetectionInitialised = false;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Selected Archive File: {SelectedArchiveFile}");

            if (!string.IsNullOrEmpty(SelectedArchiveFile))
            {
                await FetchArchivedLogs();
            }
        }

        /// <summary>
        /// Fetches the initial batch of live logs from the Backup Tool API.
        /// </summary>
        private async Task FetchInitialLogs()
        {
            IsFetchingLogs = true;
            StateHasChanged();

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Fetching Live Logs for {SelectedServer}");

            LogsResponseModel? response = await BackupToolApi.GetLogs(
                SelectedServer,
                SelectedLogType);

            if (response != null && response.Logs.Count > 0)
            {
                LogEntries = [.. response.Logs.OrderBy(l => l.Id)];
                NextAfterCursor = response.NextAfter;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Fetched {LogEntries.Count} Logs");

                IsFetchingLogs = false;
                StateHasChanged();

                if (response.NextAfter.HasValue)
                {
                    await RegisterWebhook(response.NextAfter.Value);
                }
            }

            else
            {
                IsFetchingLogs = false;
            }
        }

        /// <summary>
        /// Fetches the list of archived log files from the Backup Tool API.
        /// </summary>
        private async Task FetchArchiveList()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Fetching Archive List for {SelectedServer}");

            LogArchivesResponseModel? response = await BackupToolApi.GetLogArchives(SelectedServer);

            if (response != null)
            {
                ArchiveFiles = response.Archives;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Archives Found: {ArchiveFiles.Count}");
            }
        }

        /// <summary>
        /// Fetches logs from the selected archive file.
        /// </summary>
        private async Task FetchArchivedLogs()
        {
            IsFetchingLogs = true;
            StateHasChanged();

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Fetching Archived Logs: {SelectedArchiveFile}");

            ArchivedLogsResponseModel? response = await BackupToolApi.GetArchivedLogs(
                SelectedServer,
                SelectedArchiveFile);

            if (response != null)
            {
                LogEntries = [.. response.Logs.SelectMany(l => l.Content)
                    .OrderBy(l => l.Id)];

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Archived Logs Loaded: {LogEntries.Count}");
            }

            IsFetchingLogs = false;
        }

        /// <summary>
        /// Registers a webhook with the Backup Tool API and subscribes to the event bus.
        /// </summary>
        private async Task RegisterWebhook(int afterId)
        {
            string webhookUrl = $"{BackupToolSettings.SiteBaseUrl}/webhooks/serverlogs";

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Registering Webhook: {webhookUrl}");

            WebhookRegistrationResponseModel? registration = await BackupToolApi.RegisterWebhook(
                SelectedServer,
                webhookUrl,
                SelectedLogType,
                "All",
                afterId);

            if (registration != null)
            {
                WebhookRegistrationId = registration.Id;
                IsWebhookActive = true;

                LogStream.Subscribe(
                    SelectedServer,
                    OnLogsReceived);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Webhook Active: {WebhookRegistrationId}");

                await InvokeAsync(StateHasChanged);
            }

            else
            {
                ErrorMessage = "Failed to register webhook.";

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "Failed to Register Webhook");
            }
        }

        /// <summary>
        /// Handles incoming logs from the webhook event bus.
        /// </summary>
        private async Task OnLogsReceived(List<LogEntryModel> newLogs)
        {
            await LogLock.WaitAsync();

            try
            {
                LogEntries.AddRange(newLogs);
            }

            finally
            {
                LogLock.Release();
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called by JavaScript when the user scrolls near the top of the console.
        /// </summary>
        [JSInvokable]
        public async Task OnScrollNearTop()
        {
            if (IsFetchingOlder || NextAfterCursor == null)
            {
                return;
            }

            IsFetchingOlder = true;
            await InvokeAsync(StateHasChanged);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Loading Older Logs, Cursor: {NextAfterCursor}");

            LogsResponseModel? response = await BackupToolApi.GetLogs(
                SelectedServer,
                SelectedLogType,
                afterId: NextAfterCursor.Value);

            if (response != null && response.Logs.Count > 0)
            {
                List<LogEntryModel> olderLogs = [.. response.Logs.OrderBy(l => l.Id)];

                await LogLock.WaitAsync();

                try
                {
                    LogEntries.InsertRange(
                        0,
                        olderLogs);
                }

                finally
                {
                    LogLock.Release();
                }

                NextAfterCursor = response.NextAfter;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Loaded {olderLogs.Count} Older Logs");
            }

            else
            {
                NextAfterCursor = null;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    "No More Older Logs Available");
            }

            IsFetchingOlder = false;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Cleans up the webhook subscription and registration.
        /// </summary>
        private async Task CleanupWebhook()
        {
            if (IsWebhookActive)
            {
                LogStream.Unsubscribe(
                    SelectedServer,
                    OnLogsReceived);

                if (!string.IsNullOrEmpty(WebhookRegistrationId))
                {
                    await BackupToolApi.UnregisterWebhook(
                        SelectedServer,
                        WebhookRegistrationId);
                }

                IsWebhookActive = false;
                WebhookRegistrationId = null;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Webhook Cleaned Up");
            }
        }

        /// <summary>
        /// Disposes the component and cleans up resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await CleanupWebhook();

            DotNetRef?.Dispose();

            if (JsModule != null)
            {
                try
                {
                    await JsModule.DisposeAsync();
                }

                catch
                {
                    JsModule = null;
                }
            }

            LogLock.Dispose();

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Server Logs Page Disposed");
        }
    }
}
