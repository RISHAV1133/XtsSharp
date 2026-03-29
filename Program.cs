using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using XtsCsharpClient.Services;
using XtsCsharpClient.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace XtsCsharpClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Load Configuration (Security: Secret Management)
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string baseUrl = config["XtsSettings:BaseUrl"] ?? "";
            string apiKey = config["XtsSettings:AppKey"] ?? "";
            string secretKey = config["XtsSettings:SecretKey"] ?? "";
            string source = config["XtsSettings:Source"] ?? "WEBAPI";

            Console.WriteLine("--- XTS API C# Client (Advanced) ---");

            try
            {
                // 2. Initialize & Login (Error Handling: Authentication)
                var connector = new XtsConnector(baseUrl, apiKey, secretKey, source);
                Console.WriteLine("Logging in...");
                var loginResponse = await connector.LoginAsync();

                if (loginResponse?.result == null)
                {
                    Console.WriteLine("Login failed. Please check credentials.");
                    return;
                }

                Console.WriteLine($"Login Successful! User: {connector.UserID}");
                Console.WriteLine($"Token: {connector.Token.Substring(0, 10)}...");

                // 2. Fetch OHLC for Top 5 NIFTY 50 Constituents
                var top5Symbols = new List<string> { "RELIANCE", "HDFCBANK", "ICICIBANK", "INFOSYS", "TCS" };
                var symbolIds = new List<int>();

                Console.WriteLine("\n--- Fetching OHLC for Top 5 Nifty Constituents ---");
                foreach (var symbol in top5Symbols)
                {
                    var results = await connector.SearchInstrumentsAsync(symbol);
                    var eqResult = results.Find(r => r.Series == "EQ" && r.ExchangeSegment == 1);
                    
                    if (eqResult != null)
                    {
                        symbolIds.Add(eqResult.ExchangeInstrumentID);
                        Console.WriteLine($"\nSearching for {symbol}: Found {eqResult.Name} (ID: {eqResult.ExchangeInstrumentID})");
                        
                        try {
                            var ohlc = await connector.GetOHLCAsync(1, eqResult.ExchangeInstrumentID, "Mar 24 2026 091500", "Mar 24 2026 153000", 60);
                            if (ohlc?.result?.dataReponse != null) {
                                var count = ohlc.result.dataReponse.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                                Console.WriteLine($"   [SUCCESS] Received {count} candles (1-minute).");
                            }
                        } catch (Exception ex) { Console.WriteLine($"   [FAILED] {symbol} fetch: {ex.Message}"); }
                    }
                    else { Console.WriteLine($"\nSearching for {symbol}: No equity instrument found."); }
                }
                
                // For the demo subscription, use the first ID found
                int instrumentID = symbolIds.Count > 0 ? symbolIds[0] : 2885;
                int exchangeSegment = 1; 

                {

                    // 3. Socket connection (with timeout, non-blocking)
                    MarketDataStreamer streamer = null;
                    bool socketConnected = false;
                    
                    try
                    {
                        Console.WriteLine("\nConnecting to Market Data Socket (format: BINARY)...");
                        streamer = new MarketDataStreamer(baseUrl, connector.Token, connector.UserID);
                        
                        streamer.OnDataReceived += (eventName, data) =>
                        {
                            Console.WriteLine($"[TICK] {eventName}: {data}");
                        };

                        // Use CancellationToken for a hard timeout
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var connectTask = streamer.ConnectAsync();
                        var completedTask = await Task.WhenAny(connectTask, Task.Delay(-1, cts.Token));
                        
                        if (completedTask == connectTask)
                        {
                            await connectTask; // propagate any exception
                            socketConnected = true;
                            Console.WriteLine("Socket connected!");
                        }
                        else
                        {
                            Console.WriteLine("[Socket] Connection timed out after 10s - continuing without streaming.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Socket] Connection failed: {ex.Message} - continuing without streaming.");
                    }

                    // 4. Subscribe to the first found instrument
                    try
                    {
                        await connector.SubscribeAsync(new List<Instrument> 
                        { 
                            new Instrument { exchangeSegment = exchangeSegment, exchangeInstrumentID = instrumentID } 
                        });
                        Console.WriteLine($"Subscribed to ID: {instrumentID}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Subscription failed: {ex.Message}");
                    }

                    // 5. Fetch Monthly F&O Data (near-month) for HDFCBANK & NIFTY
                    Console.WriteLine("\n--- Monthly F&O Data Download (near-month) ---");
                    var foUnderlyings = new List<string> { "HDFCBANK", "NIFTY" };
                    foreach (var underlying in foUnderlyings)
                    {
                        Console.WriteLine($"\nSearching for {underlying} near-month Futures...");
                        var foResults = await connector.SearchInstrumentsAsync(underlying);
                        if (foResults == null) {
                            Console.WriteLine($"No results returned for {underlying}.");
                            continue;
                        }

                        // Filter for Segment 2 (NSEFO) and searching for "FUT" in Description
                        // Use a more robust check to skip spreads (which have longer names like NIFTY26MAR26MAYFUT)
                        var allFuts = foResults.FindAll(r => r.ExchangeSegment == 2 && 
                                                           r.Name == underlying &&
                                                           r.Description != null && 
                                                           r.Description.Contains("FUT"));
                        
                        // Sort by description length (shortest is usually the main future, spreads are longer)
                        allFuts.Sort((a, b) => a.Description.Length.CompareTo(b.Description.Length));
                        
                        var nearMonthFut = allFuts.Find(r => !r.Description.Contains("MAR26APR") && 
                                                            !r.Description.Contains("APR26MAY") &&
                                                            !r.Description.Contains("MAR26MAY"));

                        if (nearMonthFut != null)
                        {
                            Console.WriteLine($"Found Contract: {nearMonthFut.Name} ({nearMonthFut.Description}) ID: {nearMonthFut.ExchangeInstrumentID}");
                            try
                            {
                                // Download 1-minute data for the full month (approx)
                                var ohlcFo = await connector.GetOHLCAsync(2, nearMonthFut.ExchangeInstrumentID, "Mar 01 2026 091500", "Mar 27 2026 153000", 60);
                                if (ohlcFo?.result?.dataReponse != null)
                                {
                                    var count = ohlcFo.result.dataReponse.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                                    Console.WriteLine($"   [SUCCESS] Downloaded {count} 1-min candles for {nearMonthFut.Name}");
                                }
                            }
                            catch (Exception ex) { Console.WriteLine($"   [FAILED] F&O fetch: {ex.Message}"); }
                        }
                        else { Console.WriteLine($"No near-month Futures found for {underlying}."); }
                    }
                    Console.WriteLine("----------------------------------------------");

                    if (socketConnected)
                    {
                        Console.WriteLine("\nStreaming for 10 seconds...");
                        await Task.Delay(10000);
                        await streamer.DisconnectAsync();
                    }

                    Console.WriteLine("\n--- Demo Complete ---");
                }
            }
            catch (XtsException ex)
            {
                Console.WriteLine($"\n[XTS Error {ex.Code}]: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nCRITICAL SYSTEM ERROR: {ex.Message}");
            }
        }
    }
}
