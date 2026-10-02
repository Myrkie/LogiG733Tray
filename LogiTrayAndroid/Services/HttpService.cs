using System.Text;
using System.Text.Json;
using LogiTrayAndroid.Services.Models;

namespace LogiTrayAndroid.Services
{
    public class HttpService
    {
        private HttpClient? _client;
        public bool IsConnected => _client != null;

        private string _apiKey = "";

        public void SetConnection(string? ip, string? apiKey)
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
                (baseUri.Scheme != Uri.UriSchemeHttp &&
                 baseUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException(
                    "Address must be a valid HTTP or HTTPS address.",
                    nameof(ip));
            }

            _apiKey = apiKey;

            _client?.Dispose();

            _client = new HttpClient
            {
                BaseAddress = baseUri
            };

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
            _client.DefaultRequestHeaders.Add("User-Agent", "LogiTrayControl");
        }

        private void EnsureClient()
        {
            if (_client == null) throw new InvalidOperationException("HttpClient not initialized. Call SetConnection first.");
        }

        public async Task<StatusResponse?> GetStatusAsync()
        {
            EnsureClient();

            try
            {
                var response = await _client!.GetAsync("/status");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedException();

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync();

                    throw new HttpRequestException($"GetStatus failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
                }

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize(
                    json,
                    AppJsonContext.Default.StatusResponse);
            }
            catch (HttpRequestException ex) when (IsCertificateValidationError(ex))
            {
                throw new CertificateValidationException(
                    "The server certificate could not be trusted. " +
                    "Make sure the correct LogiG733Tray CA certificate is installed.",
                    ex);
            }
        }
        
        public async Task<int> GetAutoPowerOffAsync()
        {
            EnsureClient();

            var response = await _client!.GetAsync("/powerofftime");

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"GetAutoPowerOff failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
            }

            var json = await response.Content.ReadAsStringAsync();

            return int.TryParse(json, out int minutes) 
                ? minutes : throw new FormatException($"GetAutoPowerOff returned invalid data: {json}");
        }

        public async Task<MediaResponse?> GetMediaStatusAsync()
        {
            EnsureClient();

            var response = await _client!.GetAsync("/media-status");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedException();

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"GetMediaStatus failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize(json, AppJsonContext.Default.MediaResponse);
        }

        public async Task GetMediaNextAsync()
        {
            EnsureClient();

            var response = await _client!.PostAsync("/media-next", null);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedException();

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"GetMediaNext failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
            }

            var json = await response.Content.ReadAsStringAsync();

            JsonSerializer.Deserialize(json, AppJsonContext.Default.SessionSwitchResponse);
        }

        public async Task GetMediaPreviousAsync()
        {
            EnsureClient();

            var response = await _client!.PostAsync("/media-previous", null);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedException();

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"GetMediaPrevious failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
            }

            var json = await response.Content.ReadAsStringAsync();

            JsonSerializer.Deserialize(json, AppJsonContext.Default.SessionSwitchResponse);
        }

        public async Task<SessionPowerStateResponse?> GetMediaPowerStateAsync()
        {
            EnsureClient();

            var response = await _client!.GetAsync("/media-pwr");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedException();

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"GetMediaStateAsync failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
            }

            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize(json, AppJsonContext.Default.SessionPowerStateResponse);
        }

        public async Task SetAutoPowerOffAsync(int minutes)
        {
            EnsureClient();

            var payload = new SetAutoPowerOffRequest
            {
                Minutes = minutes
            };

            var json = JsonSerializer.Serialize(payload, AppJsonContext.Default.SetAutoPowerOffRequest);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client!.PostAsync("/powerofftime", content);

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"SetAutoPowerOff failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
            }
        }

        public async Task SetLightAsync(string target, int r, int g, int b, LightMode mode = LightMode.Static)
        {
            EnsureClient();

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

            var json = JsonSerializer.Serialize(payload, AppJsonContext.Default.SetLightRequest);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client!.PostAsync("/lights", content);

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"SetLight failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
            }
        }

        public async Task TurnOffLightsAsync()
        {
            EnsureClient();

            var response = await _client!.PostAsync("/lights/off", null);

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"TurnOffLights failed: {(int)response.StatusCode} {response.ReasonPhrase}. {text}");
            }
        }

        private static bool IsCertificateValidationError(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current.Message.Contains("Trust anchor for certification path not found", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (current.Message.Contains("CertPathValidatorException", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (current.Message.Contains("CertificateException", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        
        public class CertificateValidationException(string message, Exception? inner = null)
            : Exception(message, inner);

        public class UnauthorizedException() : Exception("Unauthorized: API key is invalid");
    }
}