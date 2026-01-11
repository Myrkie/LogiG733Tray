using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace LogiG733Tray.API
{
    public static class ApiKeyGenerator
    {
        public static string GetApiKey()
        {
            return GenerateApiKey();
        }
        
        private static string GenerateApiKey()
        {
            var machineGuid = GetMachineGuid();
            return ComputeSha256Hash(machineGuid);
        }

        private static string GetMachineGuid()
        {
            using var localMachineX64View =
                RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var rk = localMachineX64View.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            return rk?.GetValue("MachineGuid")?.ToString()
                   ?? throw new Exception("MachineGuid not found");
        }

        private static string ComputeSha256Hash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

}