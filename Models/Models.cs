using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace XtsCsharpClient.Models
{
    public class LoginRequest
    {
        public string appKey { get; set; }
        public string secretKey { get; set; }
        public string source { get; set; }
    }

    public class LoginResponse
    {
        public string type { get; set; }
        public string code { get; set; }
        public string description { get; set; }
        public LoginResult result { get; set; }
    }

    public class LoginResult
    {
        public string token { get; set; }
        public string userID { get; set; }
        public bool isInvestorClient { get; set; }
        public string publicIp { get; set; }
    }

    public class OHLCRequest
    {
        public int exchangeSegment { get; set; }
        public int exchangeInstrumentID { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public int compressionValue { get; set; }
    }

    public class OHLCResponse
    {
        public string type { get; set; }
        public string code { get; set; }
        public string description { get; set; }
        public OHLCResult result { get; set; }
    }

    public class OHLCResult
    {
        public int exchangeSegment { get; set; }
        public string exchangeInstrumentID { get; set; }
        public string dataReponse { get; set; } // Pipe-delimited OHLC data (yes, typo is in the API)
    }

    public class InstrumentSearchResponse
    {
        public string type { get; set; }
        public string code { get; set; }
        public string description { get; set; }
        public List<InstrumentSearchResult> result { get; set; }
    }

    public class InstrumentSearchResult
    {
        public int ExchangeSegment { get; set; }
        public int ExchangeInstrumentID { get; set; }
        public string InstrumentType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Series { get; set; }
        public string Symbol { get; set; }
        public string DisplayName { get; set; }
    }

    public class SubscriptionRequest
    {
        public List<Instrument> instruments { get; set; }
        public int xtsMessageCode { get; set; }
    }

    public class Instrument
    {
        public int exchangeSegment { get; set; }
        public int exchangeInstrumentID { get; set; }
    }
}
