// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;


namespace PartnerLed.Providers
{
    /// <summary>
    /// Helper class to call a protected API and process its result
    /// </summary>
    public class ProtectedApiCallHelper
    {
        private const string GRAPH_RESOURCE = "graph";
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpClient">HttpClient used to call the protected API</param>
        public ProtectedApiCallHelper(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; private set; }

        public void setHeader(string webApiUrl, string token)
        {

            var defaultRequestHeaders = HttpClient.DefaultRequestHeaders;
            Uri resourceUri = new Uri(webApiUrl);

            if (defaultRequestHeaders.Accept == null || !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
            {
                HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            }
            if (!resourceUri.Host.Contains(GRAPH_RESOURCE))
            {
                HttpClient.DefaultRequestHeaders.Add("Accept-Encoding", new List<string> { "gzip", "deflate", "br" });
            }
            defaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }



        /// <summary>
        /// Get call the protected web API and processes the result
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return JSON)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        public async Task<HttpResponseMessage> CallWebApiAndProcessResultAsync(string webApiUrl, string accessToken)
        {
            setHeader(webApiUrl, accessToken);

            HttpResponseMessage response = await HttpClient.GetAsync(webApiUrl);
            return response;
        }

        /// <summary>
        /// Post call the protected web API and processes the result
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return JSON)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        /// <param name="data">JSON data</param>
        public async Task<HttpResponseMessage> CallWebApiPostAndProcessResultAsync(string webApiUrl, string accessToken, string data)
        {
            setHeader(webApiUrl, accessToken);
            var httpContent = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
           return await HttpClient.PostAsync(webApiUrl, httpContent);
        }

        /// <summary>
        /// Post call the protected web API and processes the result
        /// </summary>
        /// <param name="webApiUrl">URL of the web API to call (supposed to return JSON)</param>
        /// <param name="accessToken">Access token used as a bearer security token to call the web API</param>
        /// <param name="data">JSON data</param>
        public async Task<HttpResponseMessage> CallWebApiPostAndProcessResultAsync(string webApiUrl, string data)
        {
            var httpContent = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
            return await HttpClient.PostAsync(webApiUrl, httpContent);
        }

        private async Task ErrorHandler(HttpResponseMessage response)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            string content = await response.Content.ReadAsStringAsync();

            if (!content.Contains("Resource 'manager' does not exist"))
            {
                Console.WriteLine($"Failed to call the Web Api: {response.StatusCode}");
                Console.WriteLine($"Content: {content}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("No manager");
            }
            Console.ResetColor();
        }
    }
}
