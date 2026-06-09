// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Converters;
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Models.Responses
{
    /// <summary>
    /// Stores the user API response.
    /// </summary>
    public class UserModel
    {
        public required int Id { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required List<string> Scopes { get; set; }
        public List<SettingModel> Settings { get; set; } = StandardValues.SettingValues.Default;

        public event Action? OnDarkModeChanged;

        public bool DarkMode
        {
            get => bool.Parse(Settings.First(s => s.Name == "DarkMode").Value);
            set
            {
                Settings.First(s => s.Name == "DarkMode").Value = value.ToString();
                OnDarkModeChanged?.Invoke();
            }
        }
    }
}
