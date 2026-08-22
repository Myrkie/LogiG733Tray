using System.Text.RegularExpressions;
using Windows.Media.Control;

namespace LogiG733Tray.Win
{
    public partial class WinMediaControl
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private GlobalSystemMediaTransportControlsSession? _selectedSession;
        public event Action? MediaChanged;

        public async Task InitializeAsync()
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

            _selectedSession = _manager.GetCurrentSession();

            if (_selectedSession != null)
                AttachSessionHandlers(_selectedSession);
        }


        private GlobalSystemMediaTransportControlsSession? GetFocusedSession()
        {
            return _selectedSession ?? _manager?.GetCurrentSession();
        }
        
        private void AttachSessionHandlers(GlobalSystemMediaTransportControlsSession session)
        {
            session.MediaPropertiesChanged += Session_MediaChanged;
            session.PlaybackInfoChanged += Session_MediaChanged;
        }
        
        private void DetachSessionHandlers(GlobalSystemMediaTransportControlsSession session)
        {
            session.MediaPropertiesChanged -= Session_MediaChanged;
            session.PlaybackInfoChanged -= Session_MediaChanged;
        }

        private void Session_MediaChanged(GlobalSystemMediaTransportControlsSession sender, object args)
        {
            MediaChanged?.Invoke();
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

                if (_selectedSession != null)
                    DetachSessionHandlers(_selectedSession);

                int currentIndex = sessions
                    .Select((session, index) => new { session, index })
                    .FirstOrDefault(x => ReferenceEquals(x.session, current))
                    ?.index ?? -1;

                int nextIndex = (currentIndex + 1) % sessions.Count;

                _selectedSession = sessions[nextIndex];

                AttachSessionHandlers(_selectedSession);

                MediaChanged?.Invoke();

                return GetSessionNameAsync(_selectedSession);
            }
            catch (Exception exception)
            {
                return Task.FromException<string?>(exception);
            }
        }

        public Task<string?> SelectPreviousSession()
        {
            try
            {
                if (_manager == null)
                    return Task.FromResult<string?>(null);

                var sessions = _manager.GetSessions();
                if (sessions.Count == 0)
                    return Task.FromResult<string?>(null);

                var current = GetFocusedSession();

                if (_selectedSession != null)
                    DetachSessionHandlers(_selectedSession);

                int currentIndex = sessions
                    .Select((session, index) => new { session, index })
                    .FirstOrDefault(x => ReferenceEquals(x.session, current))
                    ?.index ?? -1;

                int previousIndex = (currentIndex - 1 + sessions.Count) % sessions.Count;

                _selectedSession = sessions[previousIndex];

                AttachSessionHandlers(_selectedSession);

                MediaChanged?.Invoke();

                return GetSessionNameAsync(_selectedSession);
            }
            catch (Exception ex)
            {
                return Task.FromException<string?>(ex);
            }
        }
        
        private async Task<string?> GetSessionNameAsync(GlobalSystemMediaTransportControlsSession? session)
        {
            if (session == null)
                return null;

            var props = await session.TryGetMediaPropertiesAsync();
            return props?.Title;
        }
        
        public Task<string?> GetCurrentSessionNameAsync()
        {
            var session = GetFocusedSession();
            if (session == null)
                return Task.FromResult<string?>(null);

            var appId = session.SourceAppUserModelId;

            if (string.IsNullOrWhiteSpace(appId))
                return Task.FromResult<string?>(null);

            var exeMatch = AppNameRegex().Match(appId);
            if (exeMatch.Success)
                return Task.FromResult<string?>(exeMatch.Groups[1].Value);

            var cleaned = appId.Split('!').LastOrDefault();

            return Task.FromResult<string?>(!string.IsNullOrWhiteSpace(cleaned) ? cleaned : appId);
        }
        
        public async Task<string?> GetCurrentMediaNameAsync()
        {
            var session = GetFocusedSession();
            if (session == null)
                return null;

            var props = await session.TryGetMediaPropertiesAsync();
            return props.Title;
        }

        [GeneratedRegex(@"([^\\/:*?""<>|]+)\.exe", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex AppNameRegex();
    }
}