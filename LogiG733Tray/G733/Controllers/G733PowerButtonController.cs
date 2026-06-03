using LogiG733Tray.Utils;
using LogiG733Tray.Win;
using Serilog;

namespace LogiG733Tray.G733.Controllers
{
    public sealed class G733PowerButtonController(WinMediaControl media, ILogger logger) : IDisposable
    {
        private CancellationTokenSource? _clickCts;
        
        public void OnButtonPressed()
        {
            if (_clickCts != null)
            {
                _clickCts.Cancel();
                _clickCts.Dispose();
                _clickCts = null;

                _ = HandleDoubleClickAsync();
                return;
            }

            _clickCts = new CancellationTokenSource();
            var token = _clickCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(Config.Instance.PwrButtonConfig.DoubleClickDelayMs, token);

                    if (!token.IsCancellationRequested)
                    {
                        await HandleSingleClickAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                }
                finally
                {
                    _clickCts?.Dispose();
                    _clickCts = null;
                }
            }, token);
        }

        private async Task HandleSingleClickAsync()
        {
            var name = await media.GetCurrentMediaNameAsync();
            await media.TogglePlayPauseAsync();

            logger.Debug("Toggling {Media}", name);
        }

        private async Task HandleDoubleClickAsync()
        {
            var name = await media.SelectNextSession();

            logger.Debug("Changing session to {Media}", name);
        }

        public void Dispose()
        {
            _clickCts?.Cancel();
            _clickCts?.Dispose();
        }
    }
}