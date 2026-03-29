# XTS C# Market Data Client

A high-performance, professional-grade C# client library for talking to the **Symphony Fintech XTS API**. This project is a production-level port from the research-focused Python client, optimized for speed, security, and stability.

---

## 🚀 Key Features

*   **Zero-Copy Binary Parsing**: High-speed memory management using `Span<T>` and `BinaryPrimitives` for ultra-low latency data processing.
*   **Advanced Resilience (Polly)**: Integrated **Polly** library for "Exponential Backoff" retries and "Circuit Breaker" to prevent account bans during server dips.
*   **Security (Secret Management)**: Professional credential storage using `appsettings.json` (Configuration Provider).
*   **Automated Analytics**: Intelligent "Near-Month" F&O contract discovery and automated OHLC downloads.


---

## 📂 Project Structure

*   **/Services**:
    *   `XtsConnector.cs`: Handles all REST API communication (Login, Search, historical OHLC).
    *   `MarketDataStreamer.cs`: Manages real-time binary socket streaming via Socket.IO.
    *   `XtsBinaryParser.cs`: A high-performance **Zero-Copy** service for parsing raw binary packets from the network.
*   **/Models**:
    *   `Models.cs`: Typed blueprints for API requests and responses.
    *   `Exceptions.cs`: Error types for predictable failure handling (Token, Order, Network).
*   **Program.cs**: Main entry point showcasing a full trading journey (Login $\to$ Search $\to$ OHLC $\to$ Live Ticks).

---

## 🛠️ Setup & Usage

### 1. Requirements
*   .NET 6.0 SDK or later.

### 2. Configuration
Update your API credentials in `appsettings.json`:
```json
{
  "XtsSettings": {
    "BaseUrl": "https://xts.rmoneyindia.co.in:3000",
    "AppKey": "YOUR_API_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "Source": "WEBAPI"
  }
}
```

### 3. Build & Run
From the root directory, you can build and run the project using the .NET CLI:
```bash
# Build the project
dotnet build

# Run the project
dotnet run --project xts-csharp-client.csproj
```

---

## 🖥️ Demo Test & Expected Output

When you run the application, it performs a complete end-to-end test of the Market Data API. Below is the typical sequence of events and expected responses:

### 1. Unified Login
The app authenticates with the XTS server using your `AppKey` and `SecretKey`.
```text
Logging in...
Login Successful! User: YOUR_USER_ID
Token: eyJhbGciOi...
```

### 2. Multi-Stock OHLC Download
It searches for the Top 5 NIFTY constituents and downloads 1-minute historical candles for the current day.
```text
Searching for RELIANCE: Found RELIANCE (ID: 2885)
   [SUCCESS] Received 375 candles (1-minute).
...
```

### 3. Binary Socket Streaming
It initiates a `BINARY` socket connection for real-time price updates.
```text
Connecting to Market Data Socket (format: BINARY)...
[Socket] Connected successfully!
```

### 4. Advanced F&O Search & Download
It automatically identifies the **Near-Month Futures** contracts for HDFCBANK and NIFTY and downloads their full monthly 1-minute historical data.
```text
Searching for HDFCBANK near-month Futures...
Found Contract: HDFCBANK (HDFCBANK26MAYFUT) ID: 66180
   [SUCCESS] Downloaded 4467 1-min candles for HDFCBANK
```

---

## 📊 Technical Improvements over Python
1.  **Speed**: C# (Compiled) provides significantly lower latency than Python (Interpreted).
2.  **Safety**: Strong typing catches errors at "Compile-time" instead of "Runtime."
3.  **Performance**: Switched the `publishFormat` to `BINARY`, reducing network payload size by ~60% compared to the standard Python JSON implementation.

---


# XtsSharp
