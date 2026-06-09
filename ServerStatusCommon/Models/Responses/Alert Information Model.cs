// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Responses
{
    /// <summary>
    /// Stores the alert API response.
    /// </summary>
    public class AlertInformationModel
    {
        public required List<AlertModel> Entries { get; set; }
        public required int EntryCount { get; set; }
        public required int PageNumber { get; set; }
        public required int PageSize { get; set; }
        public required int TotalPageCount { get; set; }
        public required int TotalCount { get; set; }
    }
}
