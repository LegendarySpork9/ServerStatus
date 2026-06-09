// Copyright © - Unpublished - Toby Hunter
using System.Net;

namespace ServerStatusCommon.Models.Responses
{
    /// <summary>
    /// Stores the api response.
    /// </summary>
    public class ResponseModel
    {
        public required HttpStatusCode StatusCode { get; set; }
        public required string Message { get; set; }
    }
}
