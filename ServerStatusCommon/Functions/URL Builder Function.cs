// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Converters;

namespace ServerStatusCommon.Functions
{
    public static class URLBuilderFunction
    {
        /// <summary>
        /// Returns the API URL.
        /// </summary>
        public static string BuildURL(
            string baseUrl,
            string endpoint,
            object? entityId = null,
            List<KeyValuePair<string, object>>? queryParameters = null,
            bool ignoreQuery = false)
        {
            string url = $"{baseUrl}{endpoint}";
            string query = APIConverter.GetQuery(endpoint);

            if (entityId != null)
            {
                url += $"/{entityId}";
            }

            if (queryParameters != null && queryParameters.Count > 0)
            {
                if (string.IsNullOrEmpty(query))
                {
                    query = "?";

                    for (int x = 0; x < queryParameters.Count; x++)
                    {
                        KeyValuePair<string, object> queryParameter = queryParameters[x];

                        query += $"{queryParameter.Key}={Uri.EscapeDataString(queryParameter.Value.ToString())}";

                        if (x != (queryParameters.Count - 1))
                        {
                            query += "&";
                        }
                    }
                }

                else
                {
                    foreach (KeyValuePair<string, object> queryParameter in queryParameters)
                    {
                        query += $"&{queryParameter.Key}={Uri.EscapeDataString(queryParameter.Value.ToString())}";
                    }
                }
            }

            if (!ignoreQuery)
            {
                url += query;
            }

            return url;
        }
    }
}
