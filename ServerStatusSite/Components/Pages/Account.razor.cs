// Copyright © - 05/10/2025 - Toby Hunter
using Microsoft.AspNetCore.Components;
using ServerStatusCommon.Abstractions;
using ServerStatusCommon.Converters;
using ServerStatusCommon.Models.Requests.Update;
using ServerStatusCommon.Models.Responses;
using ServerStatusCommon.Models.Responses.Related;
using ServerStatusCommon.Services;
using ServerStatusSite.Converters;

namespace ServerStatusSite.Components.Pages
{
    public partial class Account : ComponentBase
    {
        [Inject]
        private ILoggerService _Logger { get; set; } = default!;
        [Inject]
        private APIService APIService { get; set; } = default!;
        [Inject]
        private UserModel User { get; set; } = default!;

        private bool IsLoading;
        private bool SaveSuccess;

        private string ErrorMessage { get; set; } = string.Empty;

        private string Username { get; set; } = string.Empty;
        private string Password { get; set; } = string.Empty;
        private string DiscordName { get; set; } = string.Empty;
        private bool DarkMode { get; set; }
        private bool Loading { get; set; } = false;

        /// <summary>
        /// Loads the user data of the logged in user.
        /// </summary>
        protected override void OnInitialized()
        {
            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Opened Home Page");

            IsLoading = true;

            Username = User.Username;
            Password = User.Password;
            DiscordName = User.Settings.Find(s => s.Name == "DiscordName")?.Value ?? User.Username;
            DarkMode = User.DarkMode;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Username: {Username}");
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Password: {Password}");
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Discord: {DiscordName}");
            _Logger.LogMessage(
                StandardValues.LoggerValues.Debug,
                $"Dark Mode: {DarkMode}");

            IsLoading = false;
        }

        /// <summary>
        /// Returns the CSS to change the page to dark mode.
        /// </summary>
        public string GetStyle(string component)
        {
            return component switch
            {
                "Form" => StyleConverter.GetFormDarkMode(User.DarkMode),
                "Input" => StyleConverter.GetInputDarkMode(User.DarkMode),
                _ => string.Empty
            };
        }

        /// <summary>
        /// Updates the user data.
        /// </summary>
        private async Task SaveClick()
        {
            SaveSuccess = false;
            ErrorMessage = string.Empty;

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "Attempting User Save");

            Loading = true;
            StateHasChanged();

            ResponseModel? apiResponse = null;

            bool performUserUpdate = false;

            UserUpdateRequestModel userUpdate = new();

            if (!string.IsNullOrWhiteSpace(Username) && Username != User.Username)
            {
                userUpdate.Username = Username;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Username: {User.Username} -> {Username}");

                performUserUpdate = true;
            }

            if (!string.IsNullOrWhiteSpace(Password) && Password != User.Password)
            {
                userUpdate.Password = Password;

                _Logger.LogMessage(
                    StandardValues.LoggerValues.Debug,
                    $"Password: {User.Password} -> {Password}");

                performUserUpdate = true;
            }

            if (performUserUpdate)
            {
                (UserModel? user, apiResponse) = await APIService.UpdateUser(
                User.Id,
                userUpdate);

                if (user != null)
                {
                    User.Username = user.Username;
                    User.Password = user.Password;
                }

                else
                {
                    ErrorMessage = $"API returned {apiResponse?.StatusCode} ({apiResponse?.Message})";

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        ErrorMessage);
                }
            }

            SettingModel discordSetting = User.Settings.First(s => s.Name == "DiscordName");

            if (!string.IsNullOrWhiteSpace(DiscordName) && DiscordName != discordSetting.Value)
            {
                UserSettingUpdateRequestModel settingUpdate = new()
                {
                    Value = DiscordName
                };

                (SettingModel? setting, apiResponse) = await APIService.UpdateUserSettings(
                    discordSetting.Id,
                    settingUpdate);

                if (setting != null)
                {
                    discordSetting.Value = setting.Value;
                }

                else
                {
                    ErrorMessage = $"API returned {apiResponse?.StatusCode} ({apiResponse?.Message})";

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        ErrorMessage);
                }
            }

            if (string.IsNullOrEmpty(ErrorMessage) && DarkMode != User.DarkMode)
            {
                SettingModel darkModeSetting = User.Settings.First(s => s.Name == "DarkMode");
                UserSettingUpdateRequestModel settingUpdate = new()
                {
                    Value = DarkMode.ToString()
                };

                (SettingModel? setting, apiResponse) = await APIService.UpdateUserSettings(
                    darkModeSetting.Id,
                    settingUpdate);

                if (setting != null)
                {
                    darkModeSetting.Value = setting.Value;
                    User.DarkMode = bool.Parse(setting.Value);
                }

                else
                {
                    ErrorMessage = $"API returned {apiResponse?.StatusCode} ({apiResponse?.Message})";

                    _Logger.LogMessage(
                        StandardValues.LoggerValues.Warning,
                        ErrorMessage);
                }
            }

            if (string.IsNullOrEmpty(ErrorMessage))
            {
                SaveSuccess = true;
            }

            _Logger.LogMessage(
                StandardValues.LoggerValues.Info,
                "User Save Complete");

            Loading = false;

            await InvokeAsync(StateHasChanged);

            await Task.Delay(2000).ContinueWith(_ =>
            {
                SaveSuccess = false;

                InvokeAsync(StateHasChanged);
            });
        }
    }
}