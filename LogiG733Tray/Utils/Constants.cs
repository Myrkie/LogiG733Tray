using System.Reflection;

namespace LogiG733Tray.Utils
{
    public static class Constants
    {
        public const string AppName = "LogiG733Tray";
        public const string AppNameAlt = "LogiTrayControl";
        private const string AppNameAltSpaced = "LogiTray Control";
        private static readonly string FormattedVersion = 
            Assembly.GetExecutingAssembly().GetName().Version is { } version
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "Unknown";
        public static readonly string AppNameAltSpacedVersioned = $"{AppNameAltSpaced} - V{FormattedVersion}";
    }
}