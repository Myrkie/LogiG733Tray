using Windows.Media.Control;

namespace LogiG733Tray.Win
{
    public class WinMediaControl
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private GlobalSystemMediaTransportControlsSession? _selectedSession;

        public async Task InitializeAsync()
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _selectedSession = _manager.GetCurrentSession();
        }

        private GlobalSystemMediaTransportControlsSession? GetFocusedSession()
        {
            return _selectedSession ?? _manager?.GetCurrentSession();
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

        public Task<string?> SelectNextSession()
        {
            try
            {
                if (_manager == null)
                    return Task.FromResult<string?>(null);

                var sessions = _manager.GetSessions();

                if (sessions.Count == 0)
                    return Task.FromResult<string?>(null);

                var current = GetFocusedSession();

                if (current == null)
                {
                    _selectedSession = sessions[0];
                    return GetSessionNameAsync(_selectedSession);
                }

                int currentIndex = sessions
                    .Select((session, index) => new { session, index })
                    .FirstOrDefault(x => ReferenceEquals(x.session, current))
                    ?.index ?? -1;

                if (currentIndex < 0)
                {
                    _selectedSession = sessions[0];
                    return GetSessionNameAsync(_selectedSession);
                }

                int nextIndex = (currentIndex + 1) % sessions.Count;
                _selectedSession = sessions[nextIndex];

                return GetSessionNameAsync(_selectedSession);
            }
            catch (Exception exception)
            {
                return Task.FromException<string?>(exception);
            }
        }
        
        private async Task<string?> GetSessionNameAsync(GlobalSystemMediaTransportControlsSession? session)
        {
            if (session == null)
                return null;

            var props = await session.TryGetMediaPropertiesAsync();
            return props?.Title;
        }
        
        public async Task<string?> GetCurrentMediaNameAsync()
        {
            var session = GetFocusedSession();
            if (session == null)
                return null;

            var props = await session.TryGetMediaPropertiesAsync();
            return props?.Title;
        }
    }
}