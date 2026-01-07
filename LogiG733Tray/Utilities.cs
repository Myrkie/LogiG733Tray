using Serilog;

namespace LogiG733Tray
{
    public class Utilities
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(Utilities));
        private const string AppName = "LogiG733Tray";
        private static Mutex _mutex = null!;
        
        internal static void SingleInstanceCheck()
        {
            Thread.Sleep(2000); // let's wait a bit to let any previous ones close before checking.
            _mutex = new Mutex(true, AppName, out var createdNew);
            if (createdNew) return;
            var str = AppName + " is already running.";
            Logger.Information(str);
            MessageBox.Show(str, AppName);
            Environment.Exit(0);
        }
    }
}