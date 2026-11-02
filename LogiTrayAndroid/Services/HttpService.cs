using LogiTrayAndroid.Services.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace LogiTrayAndroid.Services
{
    public class HttpService : IAsyncDisposable
    {
        private HubConnection? _connection;
        public bool IsConnected => _connection?.State == HubConnectionState.Connected;
        public event Action<StatusResponse>? StatusUpdated;
        public event Action<MediaResponse>? MediaUpdated;
        public event Action<SessionPowerStateResponse>? MediaPowerStateUpdated;
        public event Action<HubConnectionState>? ConnectionStateChanged;

        public void SetConnection(string? ip, string? apiKey)
        {
            if (_connection != null)
                throw new InvalidOperationException("Dispose the existing connection before setting a new one.");

            BuildConnection(ip, apiKey);
        }

        public async Task SetConnectionAsync(string? ip, string? apiKey)
        {
            var oldConnection = _connection;
            _connection = null;

            if (oldConnection != null)
                await oldConnection.DisposeAsync();

            BuildConnection(ip, apiKey);

            ConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
        }

        private void BuildConnection(string? ip, string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentException("IP address cannot be empty", nameof(ip));

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be empty", nameof(apiKey));

            var address = ip.Trim();

            if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                address = $"http://{address}";
            }

            if (!Uri.TryCreate(address, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Address must be a valid HTTP or HTTPS address.",
                    nameof(ip));
            }

            var hubUri = new Uri(baseUri, "/hub");

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUri, options =>
                {
                    options.Headers.Add("X-Api-Key", apiKey);
                    options.Headers.Add("User-Agent", "LogiTrayControl");
                })
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.TypeInfoResolver =
                        AppJsonContext.Default;
                })
                .WithAutomaticReconnect().Build();

            connection.On<StatusResponse>("StatusUpdated", status =>
            {
                StatusUpdated?.Invoke(status);
            });

            connection.On<MediaResponse>("MediaUpdated", media =>
            {
                MediaUpdated?.Invoke(media);
            });

            connection.On<SessionPowerStateResponse>("MediaPowerStateUpdated", state =>
            {
                MediaPowerStateUpdated?.Invoke(state);
            });

            connection.Reconnecting += _ =>
            {
                ConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
                return Task.CompletedTask;
            };

            connection.Reconnected += _ =>
            {
                ConnectionStateChanged?.Invoke(HubConnectionState.Connected);
                return Task.CompletedTask;
            };

            connection.Closed += _ =>
            {
                ConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
                return Task.CompletedTask;
            };

            _connection = connection;
        }

        private HubConnection GetConnection()
        {
            if (_connection == null)
                throw new InvalidOperationException("SignalR connection not initialized.");

            return _connection.State != HubConnectionState.Connected ? throw new InvalidOperationException("SignalR is not connected.") : _connection;
        }

        public async Task StartAsync()
        {
            var connection = _connection;

            if (connection == null)
                throw new InvalidOperationException("Call SetConnection first.");

            if (connection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
            {
                return;
            }

            await connection.StartAsync();

            if (ReferenceEquals(_connection, connection))
                ConnectionStateChanged?.Invoke(HubConnectionState.Connected);
        }

        public async Task StopAsync()
        {
            var connection = _connection;

            if (connection == null)
                return;

            await connection.StopAsync();
        }

        public async Task<int> GetAutoPowerOffAsync()
        {
            return await GetConnection().InvokeAsync<int>("GetPowerOffTime");
        }

        public async Task SetAutoPowerOffAsync(int minutes)
        {
            await GetConnection().InvokeAsync("SetPowerOffTime", new SetAutoPowerOffRequest { Minutes = minutes });
        }

        public async Task GetMediaNextAsync()
        {
            await GetConnection().InvokeAsync<SessionSwitchResponse>("MediaNext");
        }

        public async Task GetMediaPreviousAsync()
        {
            await GetConnection().InvokeAsync<SessionSwitchResponse>("MediaPrevious");
        }

        public async Task SetLightAsync(string target, int r, int g, int b, LightMode mode = LightMode.Static)
        {
            var payload = new SetLightRequest
            {
                Target = target,
                UpperR = r,
                UpperG = g,
                UpperB = b,
                LowerR = r,
                LowerG = g,
                LowerB = b,
                Mode = mode
            };

            await GetConnection().InvokeAsync("SetLights", payload);
        }

        public async Task TurnOffLightsAsync()
        {
            await GetConnection().InvokeAsync("TurnOffLights");
        }

        public async ValueTask DisposeAsync()
        {
            var connection = _connection;
            _connection = null;

            if (connection != null)
                await connection.DisposeAsync();

            ConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
        }
    }
}