// Copyright © - Unpublished - Toby Hunter
using RestSharp;

namespace ServerStatusCommon.Abstractions
{
    /// <summary>
    /// Interface for the REST client operations.
    /// </summary>
    public interface IRestClientWrapper
    {
        Task<RestResponse> ExecuteAsync(string url, RestRequest request);
    }
}
