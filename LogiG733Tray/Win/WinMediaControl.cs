using Windows.Media.Control;

namespace LogiG733Tray.Win
{
    public class WinMediaControl
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;

        public async Task InitializeAsync()
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        }

        private GlobalSystemMediaTransportControlsSession? GetFocusedSession()
        {
            return _manager?.GetCurrentSession();
        }

        public async Task TogglePlayPauseAsync()
        {
            var session = GetFocusedSession();
            if (session == null)
                return;

            var playbackInfo = session.GetPlaybackInfo();

            if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                await session.TryPauseAsync();
            }
            else
            {
                await session.TryPlayAsync();
            }
        }
    }
}