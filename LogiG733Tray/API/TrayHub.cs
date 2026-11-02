using LogiG733Tray.API.Models;
using LogiG733Tray.G733;
using LogiG733Tray.Utils;
using LogiG733Tray.Win;
using Microsoft.AspNetCore.SignalR;

namespace LogiG733Tray.API
{
    public class TrayHub(G733Device device, G733BatteryMonitor batteryMonitor, WinMediaControl? media) : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("StatusUpdated", new StatusResponse(device.Name, device.ConnectionState, batteryMonitor.LatestBattery));

            var mediaPower = Config.Instance.PwrButtonConfig.PwrPausesMedia;
            await Clients.Caller.SendAsync("MediaPowerStateUpdated", new SessionPowerStateResponse(mediaPower));

            if (mediaPower && media != null)
            {
                var session = await media.GetCurrentSessionNameAsync();
                var mediaName = await media.GetCurrentMediaNameAsync();

                await Clients.Caller.SendAsync("MediaUpdated", new MediaResponse(session, mediaName ?? "", $"{session} - {mediaName}"));
            }

            await base.OnConnectedAsync();
        }

        public StatusResponse GetStatus()
        {
            return new StatusResponse(device.Name, device.ConnectionState, batteryMonitor.LatestBattery);
        }

        public int GetPowerOffTime()
        {
            switch (device.ConnectionState)
            {
                case DeviceConnectionState.HeadsetSleeping:
                    throw new HubException("Device is asleep");

                case DeviceConnectionState.NoReceiver:
                    throw new HubException("Device receiver is not connected");

                default:
                    return device.GetAutoPowerOff();
            }
        }

        public void SetPowerOffTime(PowerOffRequest req)
        {
            device.SetAutoPowerOff(req.Minutes);
        }

        public void SetLights(LightRequest req)
        {
            var mode = req.Mode ?? G733HidClient.LightMode.Static;

            switch (req.Target.ToLower())
            {
                case "upper":
                    device.SetUpperLightBar(
                        new G733HidClient.RgbColor(req.UpperR ?? 0, req.UpperG ?? 0, req.UpperB ?? 0), mode);
                    break;

                case "lower":
                    device.SetLowerLightBar(
                        new G733HidClient.RgbColor(req.LowerR ?? 0, req.LowerG ?? 0, req.LowerB ?? 0), mode);
                    break;

                case "both":
                    var upper = new G733HidClient.RgbColor(req.UpperR ?? 0, req.UpperG ?? 0, req.UpperB ?? 0);
                    var lower = new G733HidClient.RgbColor(req.LowerR ?? 0, req.LowerG ?? 0, req.LowerB ?? 0);
                    device.SetLights(upper, lower, mode);
                    break;

                default:
                    throw new HubException("Invalid light target");
            }
        }

        public void TurnOffLights()
        {
            device.DisableLights();
        }

        public async Task<MediaResponse> GetMediaStatus()
        {
            if (media == null)
                throw new HubException("Media control is unavailable");

            var session = await media.GetCurrentSessionNameAsync();
            var mediaName = await media.GetCurrentMediaNameAsync();

            return new MediaResponse(session, mediaName ?? "", $"{session} - {mediaName}");
        }

        public async Task<SessionSwitchResponse> MediaNext()
        {
            if (media == null)
                throw new HubException("Media control is unavailable");

            await media.SelectNextSession();
            return await GetCurrentSessionResponse();
        }

        public async Task<SessionSwitchResponse> MediaPrevious()
        {
            if (media == null)
                throw new HubException("Media control is unavailable");

            await media.SelectPreviousSession();
            return await GetCurrentSessionResponse();
        }

        private async Task<SessionSwitchResponse> GetCurrentSessionResponse()
        {
            var session = await media!.GetCurrentSessionNameAsync();
            var mediaName = await media.GetCurrentMediaNameAsync();

            var display = $"{session} - {mediaName}";
            await Clients.All.SendAsync("MediaUpdated",
                new MediaResponse(session, mediaName ?? "", display));

            return new SessionSwitchResponse(display);
        }
    }
}