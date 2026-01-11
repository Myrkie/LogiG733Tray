using Android.Util;

namespace LogiTrayAndroid
{
    public static class Logger
    {
        private const string Tag = "LogiTrayAndroidDBG";

        public static void Info(string message) => Log.Info(Tag, message);

        public static void Error(string message, Exception? ex = null)
        {
            Log.Error(Tag, ex != null ? $"{message}: {ex}" : message);
        }
    }
}