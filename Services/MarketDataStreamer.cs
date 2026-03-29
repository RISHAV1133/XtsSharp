using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocketIOClient;
using SocketIOClient.Transport;
using Newtonsoft.Json;

namespace XtsCsharpClient.Services
{
    public class MarketDataStreamer
    {
        private readonly string _baseUrl;
        private readonly string _token;
        private readonly string _userID;
        private SocketIOClient.SocketIO _socket;

        public event Action<string, string> OnDataReceived;
        public event Action OnConnected;
        public event Action<string> OnError;

        public MarketDataStreamer(string baseUrl, string token, string userID)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _token = token;
            _userID = userID;
        }

        public async Task ConnectAsync()
        {
            var options = new SocketIOOptions
            {
                Path = "/apimarketdata/socket.io",
                Transport = TransportProtocol.WebSocket,
                ConnectionTimeout = TimeSpan.FromSeconds(15),
                ReconnectionAttempts = 3,
                Query = new Dictionary<string, string>
                {
                    { "token", _token },
                    { "userID", _userID },
                    { "publishFormat", "BINARY" },
                    { "broadcastMode", "Full" }
                }
            };

            _socket = new SocketIOClient.SocketIO(_baseUrl, options);

            _socket.OnConnected += (sender, e) =>
            {
                Console.WriteLine("[Socket] Connected successfully!");
                OnConnected?.Invoke();
            };

            _socket.OnError += (sender, e) =>
            {
                Console.WriteLine($"[Socket] Error: {e}");
                OnError?.Invoke(e);
            };

            _socket.OnReconnectAttempt += (sender, e) =>
            {
                Console.WriteLine($"[Socket] Reconnect attempt #{e}");
            };

            RegisterHandler("1501-binary-full");
            RegisterHandler("1502-binary-full");
            RegisterHandler("1505-binary-full");
            RegisterHandler("1512-binary-full");

            try
            {
                await _socket.ConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Socket] Connection failed: {ex.Message}");
                throw;
            }
        }

        private void RegisterHandler(string eventName)
        {
            _socket.On(eventName, (response) =>
            {
                var data = response.ToString();
                OnDataReceived?.Invoke(eventName, data);
            });
        }

        public async Task DisconnectAsync()
        {
            if (_socket != null)
            {
                await _socket.DisconnectAsync();
            }
        }
    }
}
