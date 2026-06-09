// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Models.Responses.Related;

namespace ServerStatusCommon.Models.Responses
{
    /// <summary>
    /// Stores the user settings API response.
    /// </summary>
    public class UserSettingModel
    {
        public required string Application { get; set; }
        public required List<SettingModel> Settings { get; set; }
    }
}
