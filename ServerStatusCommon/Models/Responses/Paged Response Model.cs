// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Responses
{
    /// <summary>
    /// Stores the paged API response.
    /// </summary>
    public class PagedResponseModel<T>
    {
        public required List<T> Entries { get; set; }
        public required int EntryCount { get; set; }
        public required int PageNumber { get; set; }
        public required int PageSize { get; set; }
        public required int TotalPageCount { get; set; }
        public required int TotalCount { get; set; }
    }
}
