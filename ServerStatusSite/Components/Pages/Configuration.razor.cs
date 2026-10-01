// Copyright © - Unpublished - Toby Hunter
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Values;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusSite.Abstractions;
using ServerStatusSite.Converters;
using ServerStatusSite.Models;

namespace ServerStatusSite.Components.Pages
{
    public partial class Configuration : ComponentBase
    {
        [Inject]
        private ILoggerService _Logger { get; set; } = default!;
        [Inject]
        private IJSRuntime JS { get; set; } = default!;
        [Inject]
        private IWebHostEnvironment Environment { get; set; } = default!;
        [Inject]
        private IExtendedFileSystem FileSystem { get; set; } = default!;
        [Inject]
        private BackupToolSettingsModel BackupToolSettings { get; set; } = default!;
        [Inject]
        private UserModel User { get; set; } = default!;
        [Inject]
        private NavigationManager Navigation { get; set; } = default!;

        private bool IsAuthorised;
        private bool IsAdding;
        private bool SaveSuccess;
        private bool WebhookSuccess;

        private string ErrorMessage { get; set; } = string.Empty;

        private string NewServerName { get; set; } = string.Empty;
        private string NewClientId { get; set; } = string.Empty;
        private string NewClientSecret { get; set; } = string.Empty;

        private string? EditingServerName;
        private string EditClientId { get; set; } = string.Empty;
        private string EditClientSecret { get; set; } = string.Empty;

        private string? GeneratedSecret;

        /// <summary>
        /// Checks whether the user is authorised to access this page.
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Opened Configuration Page");

            SettingModel? adminSetting = User.Settings.FirstOrDefault(s => s.Name == "IsAdmin");
            IsAuthorised = adminSetting != null && bool.TryParse(
                adminSetting.Value,
                out bool isAdmin) && isAdmin;

            if (!IsAuthorised)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    "Unauthorised Access Attempt to Configuration Page");

                await Task.Delay(2000);

                Navigation.NavigateTo("/");

                return;
            }
        }

        /// <summary>
        /// Returns the CSS to change the page to dark mode.
        /// </summary>
        private string GetStyle(string component)
        {
            return component switch
            {
                "Form" => StyleConverter.GetFormDarkMode(User.DarkMode),
                "Input" => StyleConverter.GetInputDarkMode(User.DarkMode),
                "Table" => StyleConverter.GetTableDarkMode(User.DarkMode),
                "Data" => StyleConverter.GetTableRowDarkMode(User.DarkMode),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Generates a new webhook secret using SHA-256.
        /// </summary>
        private async Task GenerateWebhookSecret()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Generating Webhook Secret");

            byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
            string rawSecret = Convert.ToHexString(randomBytes)
                .ToLowerInvariant();

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret));
            string encryptedSecret = Convert.ToHexString(hash)
                .ToLowerInvariant();

            GeneratedSecret = rawSecret;
            BackupToolSettings.WebhookSecret = encryptedSecret;

            await PersistSettings();

            WebhookSuccess = true;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Webhook Secret Generated");

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Copies the generated webhook secret to the clipboard.
        /// </summary>
        private async Task CopySecret()
        {
            if (!string.IsNullOrEmpty(GeneratedSecret))
            {
                await JS.InvokeVoidAsync(
                    "navigator.clipboard.writeText",
                    GeneratedSecret);

                GeneratedSecret = null;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    "Webhook Secret Copied to Clipboard");
            }
        }

        /// <summary>
        /// Toggles the add server form visibility.
        /// </summary>
        private void ToggleAddServer()
        {
            IsAdding = !IsAdding;
            NewServerName = string.Empty;
            NewClientId = string.Empty;
            NewClientSecret = string.Empty;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Adds a new server to the configuration.
        /// </summary>
        private async Task AddServer()
        {
            SaveSuccess = false;
            ErrorMessage = string.Empty;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Adding Server");

            if (string.IsNullOrWhiteSpace(NewServerName))
            {
                ErrorMessage = "Server name is required.";

                return;
            }

            if (string.IsNullOrWhiteSpace(NewClientId))
            {
                ErrorMessage = "Client ID is required.";

                return;
            }

            if (string.IsNullOrWhiteSpace(NewClientSecret))
            {
                ErrorMessage = "Client Secret is required.";

                return;
            }

            if (BackupToolSettings.Servers.ContainsKey(NewServerName))
            {
                ErrorMessage = "A server with that name already exists.";

                return;
            }

            BackupToolSettings.Servers[NewServerName] = new ServerCredentialsModel
            {
                ClientId = NewClientId,
                ClientSecret = NewClientSecret
            };

            await PersistSettings();

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Server '{NewServerName}' Added");

            NewServerName = string.Empty;
            NewClientId = string.Empty;
            NewClientSecret = string.Empty;

            IsAdding = false;
            SaveSuccess = true;

            await InvokeAsync(StateHasChanged);

            await Task.Delay(2000).ContinueWith(_ =>
            {
                SaveSuccess = false;

                InvokeAsync(StateHasChanged);
            });
        }

        /// <summary>
        /// Sets the specified server row into edit mode.
        /// </summary>
        private void EditServer(string name)
        {
            EditingServerName = name;
            EditClientId = BackupToolSettings.Servers[name].ClientId;
            EditClientSecret = BackupToolSettings.Servers[name].ClientSecret;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Editing Server '{name}'");
        }

        /// <summary>
        /// Saves the edited server credentials.
        /// </summary>
        private async Task SaveEdit(string name)
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(EditClientId))
            {
                ErrorMessage = "Client ID is required.";

                return;
            }

            if (string.IsNullOrWhiteSpace(EditClientSecret))
            {
                ErrorMessage = "Client Secret is required.";

                return;
            }

            BackupToolSettings.Servers[name].ClientId = EditClientId;
            BackupToolSettings.Servers[name].ClientSecret = EditClientSecret;

            await PersistSettings();

            EditingServerName = null;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Server '{name}' Updated");

            SaveSuccess = true;

            await InvokeAsync(StateHasChanged);

            await Task.Delay(2000).ContinueWith(_ =>
            {
                SaveSuccess = false;

                InvokeAsync(StateHasChanged);
            });
        }

        /// <summary>
        /// Cancels the current edit operation.
        /// </summary>
        private void CancelEdit()
        {
            EditingServerName = null;
            EditClientId = string.Empty;
            EditClientSecret = string.Empty;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Removes a server from the configuration.
        /// </summary>
        private async Task RemoveServer(string name)
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Removing Server '{name}'");

            BackupToolSettings.Servers.Remove(name);

            await PersistSettings();

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                $"Server '{name}' Removed");

            SaveSuccess = true;

            await InvokeAsync(StateHasChanged);

            await Task.Delay(2000).ContinueWith(_ =>
            {
                SaveSuccess = false;

                InvokeAsync(StateHasChanged);
            });
        }

        /// <summary>
        /// Persists the current BackupToolAPI settings to the appsettings file.
        /// </summary>
        private async Task PersistSettings()
        {
            string fileName = Environment.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";

            string filePath = Path.Combine(
                Environment.ContentRootPath,
                fileName);

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Persisting settings to {fileName}");

            try
            {
                string json = await FileSystem.ReadAllText(filePath);

                JsonNode? root = JsonNode.Parse(json);

                if (root == null)
                {
                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        "Failed to parse appsettings file");

                    return;
                }

                if (root["BackupToolAPI"] == null)
                {
                    root["BackupToolAPI"] = new JsonObject();
                }

                root["BackupToolAPI"]!["WebhookSecret"] = BackupToolSettings.WebhookSecret;

                JsonObject serversNode = [];

                foreach (var server in BackupToolSettings.Servers)
                {
                    serversNode[server.Key] = new JsonObject
                    {
                        ["ClientId"] = server.Value.ClientId,
                        ["ClientSecret"] = server.Value.ClientSecret
                    };
                }

                root["BackupToolAPI"]!["Servers"] = serversNode;

                string updatedJson = root.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await FileSystem.WriteAllText(
                    filePath,
                    updatedJson);

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Info,
                    $"Settings persisted to {fileName}");
            }

            catch (Exception ex)
            {
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Warning,
                    $"Failed to persist settings: {ex.Message}");
                _Logger.LogMessage(
                    StandardValues.LoggerValues.Error,
                    ex.ToString());

                ErrorMessage = "Failed to save settings to file.";
            }
        }
    }
}
