// Copyright © - Unpublished - Toby Hunter
using RestSharp;
using ServerStatusCommon.Abstractions;

namespace ServerStatusCommon.Implementations
{
    public class RestClientWrapper : IRestClientWrapper
    {
        /// <summary>
        /// Executes the given request against the given URL.
        /// </summary>
        public async Task<RestResponse> ExecuteAsync(
            string url,
            RestRequest request)
        {
            RestClient client = new(url);
            return await client.ExecuteAsync(request);
        }
    }
}
