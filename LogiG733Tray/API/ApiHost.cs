using LogiG733Tray.API.Models;
using LogiG733Tray.G733;
using LogiG733Tray.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LogiG733Tray.API
{
    public class ApiHost(G733Device device, G733BatteryMonitor batteryMonitor)
    {
        public void Start()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(
                    0, ApiJsonContext.Default);
            });

            var localip = Utilities.GetLocalIp();
            var localport = Config.Instance.ApiConfig.Port;

            builder.WebHost.UseUrls($"http://{localip}:{localport}");
            builder.Host.UseSerilog();
            
            var app = builder.Build();

            app.Use(async (ctx, next) =>
            {
                var apiKeyValid = ctx.Request.Headers["X-Api-Key"] == ApiKeyGenerator.GetApiKey();
                var userAgentValid = ctx.Request.Headers.UserAgent == "LogiTrayControl";

                if (!apiKeyValid || !userAgentValid)
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "text/html; charset=utf-8";

                    string html = Utilities.BuildEmbeddedPage("Unauthorized");

                    await ctx.Response.WriteAsync(html);

                    return;
                }

                await next();
            });
            
            app.MapGet("/status", () =>
            {
                var status = new StatusResponse(device.Name, device.ConnectionState, batteryMonitor.LatestBattery);
                return Results.Ok(status);
            });
            
            app.MapGet("/powerofftime", () =>
            {
                switch (device.ConnectionState)
                {
                    case DeviceConnectionState.HeadsetSleeping:
                        return Results.NotFound("Device is asleep");
                    case DeviceConnectionState.NoReceiver:
                        return Results.NotFound("Device receiver is not connected");
                    case DeviceConnectionState.ReceiverPresent:
                    case DeviceConnectionState.HeadsetOnline:
                    default:
                    {
                        var minutes = device.GetAutoPowerOff();
                        return Results.Ok(minutes);
                    }
                }
            });
            
            app.MapPost("/powerofftime", (PowerOffRequest req) =>
            {
                device.SetAutoPowerOff(req.Minutes);
                return Results.Ok();
            });

            app.MapPost("/lights", (LightRequest req) =>
            {
                var mode = req.Mode ?? G733HidClient.LightMode.Static;
                
                switch (req.Target.ToLower())
                {
                    case "upper":
                        device.SetUpperLightBar(new G733HidClient.RgbColor(req.UpperR ?? 0, req.UpperG ?? 0, req.UpperB ?? 0), mode);
                        break;

                    case "lower":
                        device.SetLowerLightBar(new G733HidClient.RgbColor(req.LowerR ?? 0, req.LowerG ?? 0, req.LowerB ?? 0), mode);
                        break;

                    case "both":
                        var upper = new G733HidClient.RgbColor(req.UpperR ?? 0, req.UpperG ?? 0, req.UpperB ?? 0);
                        var lower = new G733HidClient.RgbColor(req.LowerR ?? 0, req.LowerG ?? 0, req.LowerB ?? 0);
                        device.SetLights(upper, lower, mode);
                        break;

                    default:
                        return Results.BadRequest();
                }

                return Results.Ok();
            });
            app.MapPost("/lights/off", () =>
            {
                device.DisableLights();
                return Results.Ok();
            });

            app.RunAsync();
        }
    }
}