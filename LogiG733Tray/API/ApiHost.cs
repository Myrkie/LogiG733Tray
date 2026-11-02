using System.Net;
using LogiG733Tray.API.Models;
using LogiG733Tray.G733;
using LogiG733Tray.Utils;
using LogiG733Tray.Win;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LogiG733Tray.API
{
    public class ApiHost(G733Device device, G733BatteryMonitor batteryMonitor, WinMediaControl? media) : IDisposable
    {
        private WebApplication? _app;
        private IHubContext<TrayHub>? _hubContext;
        private static readonly bool IsHttps = Config.Instance.ApiConfig.IsHttps;

        public void Dispose()
        {
            if (_app == null) return;

            device.ConnectionStateChanged -= OnConnectionStateChanged;
            batteryMonitor.BatteryUpdated -= OnBatteryUpdated;

            if (media != null)
                media.MediaChanged -= OnMediaChanged;

            _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _app = null;
        }

        public void Start()
        {
            var builder = WebApplication.CreateBuilder();

            builder.Services.AddSingleton(device);
            builder.Services.AddSingleton(batteryMonitor);

            if (media != null)
                builder.Services.AddSingleton(media);

            builder.Services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.TypeInfoResolver = ApiJsonContext.Default;
                });

            var localIp = Utilities.GetLocalIp();
            var localPort = Config.Instance.ApiConfig.Port;

            if (!IsHttps)
            {
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.Listen(IPAddress.Parse(localIp), localPort);
                });
            }
            else
            {
                var certificate = CertificateUtils.GetOrCreateServerCertificate(localIp);

                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.Listen(IPAddress.Parse(localIp), localPort, listenOptions =>
                    {
                        listenOptions.UseHttps(certificate);
                    });
                });
            }

            builder.Host.UseSerilog();

            _app = builder.Build();

            _app.Use(async (ctx, next) =>
            {
                var apiKeyValid = ctx.Request.Headers["X-Api-Key"] == ApiKeyGenerator.GetApiKey();
                var userAgentValid = ctx.Request.Headers.UserAgent == Constants.AppNameAlt;

                if (!apiKeyValid || !userAgentValid)
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "text/html; charset=utf-8";
                    await ctx.Response.WriteAsync(Utilities.BuildEmbeddedPage("Unauthorized"));
                    return;
                }

                await next();
            });

            _hubContext = _app.Services.GetRequiredService<IHubContext<TrayHub>>();

            device.ConnectionStateChanged += OnConnectionStateChanged;
            batteryMonitor.BatteryUpdated += OnBatteryUpdated;

            if (media != null)
                media.MediaChanged += OnMediaChanged;

            _app.MapHub<TrayHub>("/hub");

            _ = _app.RunAsync();
        }

        private void OnConnectionStateChanged(DeviceConnectionState state)
        {
            BroadcastStatus();
        }

        private void OnBatteryUpdated(BatteryInfo? battery)
        {
            BroadcastStatus(battery);
        }

        private void OnMediaChanged()
        {
            _ = BroadcastMediaAsync();
        }

        private async Task BroadcastMediaAsync()
        {
            if (_hubContext == null || media == null) return;

            try
            {
                var session = await media.GetCurrentSessionNameAsync();
                var mediaName = await media.GetCurrentMediaNameAsync();

                var response = new MediaResponse(session, mediaName ?? "", $"{session} - {mediaName}");

                await _hubContext.Clients.All.SendAsync("MediaUpdated", response);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to broadcast media update");
            }
        }

        private void BroadcastStatus(BatteryInfo? battery = null)
        {
            if (_hubContext == null) return;

            var status = new StatusResponse(device.Name, device.ConnectionState, battery ?? batteryMonitor.LatestBattery);

            _ = _hubContext.Clients.All.SendAsync("StatusUpdated", status);
        }
    }
}