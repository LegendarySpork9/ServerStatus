// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Models.Responses
{
    /// <summary>
    /// Stores the component configuration API response.
    /// </summary>
    public class ComponentModel
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
}
