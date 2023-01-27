﻿using Newtonsoft.Json;
using RestSharp;

namespace BibleIndexer.Data
{
    internal static class Api
    {
        
        private static RestClient? _client;

        /// <Summary></Summary>:
        private static RestClient GetClient()
        {
            _client = null ?? new RestClient("https://gist.githubusercontent.com/king-Alex-d-great/b32f98847970708f4fbba9c94cd9a3a1/raw/97459a7dc59eaeff42c7f5d22cf1553208430e9f/");
            return _client;
        }

        
        /// <Summary>Get bible books, chapters and verses in JSON format</Summary>:
        public static async Task<List<dynamic>> GetBibleBlob()
        {
            RestClient client = GetClient();
            RestRequest request = new("kjv.json");

            RestResponse response = await client.ExecuteGetAsync(request);
            if (!response.IsSuccessful)
            {
                throw new HttpRequestException("Error: API call failed\nTip: Check that you are connected to the internet");
            }

            return JsonConvert.DeserializeObject<List<dynamic>>(response.Content);
        }
    }
}
