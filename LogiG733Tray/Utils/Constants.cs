using System.Reflection;

namespace LogiG733Tray.Utils
{
    public static class Constants
    {
        public const string AppName = "LogiG733Tray";
        public const string AppNameAlt = "LogiTrayControl";
        private const string AppNameAltSpaced = "LogiTray Control";
        private static readonly string FormattedVersion = 
            $"{Assembly.GetExecutingAssembly().GetName().Version!.Major}." +
            $"{Assembly.GetExecutingAssembly().GetName().Version!.Minor}." +
            $"{Assembly.GetExecutingAssembly().GetName().Version!.Build}";
        public static readonly string AppNameAltSpacedVersioned = $"{AppNameAltSpaced} - V{FormattedVersion}";
    }
}