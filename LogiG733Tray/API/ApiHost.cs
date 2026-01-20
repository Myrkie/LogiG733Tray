using LogiG733Tray.G733;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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

            builder.WebHost.UseUrls("http://0.0.0.0:5180");

            var app = builder.Build();

            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Headers["X-Api-Key"] != ApiKeyGenerator.GetApiKey() || ctx.Request.Headers.UserAgent != "LogiTrayControl")
                {
                    ctx.Response.StatusCode = 401;
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