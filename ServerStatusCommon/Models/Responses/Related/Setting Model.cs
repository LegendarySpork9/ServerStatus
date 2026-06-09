// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Responses.Related
{
    /// <summary>
    /// Stores the setting data.
    /// </summary>
    public class SettingModel
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string Value { get; set; }
    }
}
