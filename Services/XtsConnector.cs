using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;
using XtsCsharpClient.Models;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;

namespace XtsCsharpClient.Services
{
    public class XtsConnector
    {
        private readonly string _baseUrl;
        private readonly string _appKey;
        private readonly string _secretKey;
        private readonly string _source;
        private string _token;
        private string _userID;
        private readonly RestClient _client;
        private readonly AsyncRetryPolicy _retryPolicy;
        private static readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy = 
            Policy.Handle<Exception>()
                  .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)); // Breaks after 3 failures

        public XtsConnector(string baseUrl, string appKey, string secretKey, string source = "WEBAPI")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _appKey = appKey;
            _secretKey = secretKey;
            _source = source;
            _client = new RestClient(_baseUrl);

            // Resiliency: Exponential Backoff (1s, 2s, 4s...)
            _retryPolicy = Policy.Handle<Exception>()
                                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(Math.Pow(2, i)));
        }

        public string Token => _token;
        public string UserID => _userID;

        public async Task<LoginResponse> LoginAsync()
        {
            var request = new RestRequest("apimarketdata/auth/login", Method.Post);
            request.AddJsonBody(new
            {
                appKey = _appKey,
                secretKey = _secretKey,
                source = _source
            });

            // Use Polly: Circuit Breaker + Retry
            var response = await _circuitBreakerPolicy.ExecuteAsync(() => 
                _retryPolicy.ExecuteAsync(() => _client.ExecuteAsync(request)));

            if (!response.IsSuccessful)
            {
                throw new XtsTokenException($"Login failed: {response.StatusCode} - {response.Content}", (int)response.StatusCode);
            }

            var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(response.Content);
            if (loginResponse?.result != null)
            {
                _token = loginResponse.result.token;
                _userID = loginResponse.result.userID;
            }

            return loginResponse;
        }

        public async Task<List<InstrumentSearchResult>> SearchInstrumentsAsync(string searchString)
        {
            var request = new RestRequest("apimarketdata/search/instruments", Method.Get);
            request.AddHeader("Authorization", _token);
            request.AddParameter("searchString", searchString);

            var response = await _client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                throw new XtsDataException($"Search failed: {response.StatusCode} - {response.Content}", (int)response.StatusCode);
            }

            var searchResponse = JsonConvert.DeserializeObject<InstrumentSearchResponse>(response.Content);
            return searchResponse?.result ?? new List<InstrumentSearchResult>();
        }

        public async Task<OHLCResponse> GetOHLCAsync(int segment, int instrumentID, string startTime, string endTime, int compressionValue = 60)
        {
            var request = new RestRequest("apimarketdata/instruments/ohlc", Method.Get);
            request.AddHeader("Authorization", _token);
            request.AddParameter("exchangeSegment", segment);
            request.AddParameter("exchangeInstrumentID", instrumentID);
            request.AddParameter("startTime", startTime);
            request.AddParameter("endTime", endTime);
            request.AddParameter("compressionValue", compressionValue);

            var response = await _client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                throw new XtsNetworkException($"OHLC fetch failed: {response.StatusCode} - {response.Content}", (int)response.StatusCode);
            }

            return JsonConvert.DeserializeObject<OHLCResponse>(response.Content);
        }

        public async Task<bool> SubscribeAsync(List<Instrument> instruments, int messageCode = 1501)
        {
            var request = new RestRequest("apimarketdata/instruments/subscription", Method.Post);
            request.AddHeader("Authorization", _token);
            request.AddJsonBody(new
            {
                instruments = instruments,
                xtsMessageCode = messageCode
            });

            var response = await _client.ExecuteAsync(request);
            return response.IsSuccessful;
        }
    }
}
