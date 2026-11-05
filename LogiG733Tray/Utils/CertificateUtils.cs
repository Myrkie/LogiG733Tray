using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Serilog;

namespace LogiG733Tray.Utils
{
    public static class CertificateUtils
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(CertificateUtils));

        private const string CaName = $"{Constants.AppName} Certificate Authority";
        private const string OuName = $"{Constants.AppName} Desktop Application";
        private const string ScName = $"{Constants.AppName} API Server Certificate";

        private static string CertificateDirectory => Path.Combine(AppContext.BaseDirectory, "Config", "Cert");

        private static string CaPath => Path.Combine(CertificateDirectory, "ca.pfx");
        private static string CaPemPath => Path.Combine(CertificateDirectory, "ca.pem");
        private static string ServerCertificatePath => Path.Combine(CertificateDirectory, "api-cert.pfx");

        /// <summary>
        /// Gets the API server certificate, creating the local CA and
        /// server certificate if necessary.
        /// </summary>
        public static X509Certificate2 GetOrCreateServerCertificate(string ipAddress)
        {
            Directory.CreateDirectory(CertificateDirectory);

            var ca = GetOrCreateCaCertificate();

            if (File.Exists(ServerCertificatePath))
            {
                var existing = LoadCertificate(ServerCertificatePath);

                if (CertificateMatchesIp(existing, ipAddress))
                {
                    return existing;
                }

                existing.Dispose();

                File.Delete(ServerCertificatePath);
            }

            var serverCertificate = CreateServerCertificate(ca, ipAddress);

            var pfx = serverCertificate.Export(X509ContentType.Pfx);

            File.WriteAllBytes(ServerCertificatePath, pfx);

            return X509CertificateLoader.LoadPkcs12(pfx, password: null, keyStorageFlags: X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
        }

        /// <summary>
        /// Creates the local root CA if it doesn't already exist
        /// </summary>
        private static X509Certificate2 GetOrCreateCaCertificate()
        {
            X509Certificate2 ca;

            if (File.Exists(CaPath))
            {
                ca = LoadCertificate(CaPath);
            }
            else
            {
                ca = CreateCaCertificate();

                var pfx = ca.Export(X509ContentType.Pfx);
            
                File.WriteAllBytes(CaPath, pfx);

                ca.Dispose();

                ca = X509CertificateLoader.LoadPkcs12(pfx, password: null, keyStorageFlags:
                    X509KeyStorageFlags.Exportable |
                    X509KeyStorageFlags.MachineKeySet);
            }
            
            WriteCertificatePem(ca, CaPemPath);
            
            InstallCaCertificate(ca);

            return ca;
        }

        private static void WriteCertificatePem(X509Certificate2 certificate, string path)
        {
            var der = certificate.Export(X509ContentType.Cert);
            var base64 = Convert.ToBase64String(der);

            var sb = new StringBuilder();

            sb.AppendLine("-----BEGIN CERTIFICATE-----");

            for (var i = 0; i < base64.Length; i += 64)
            {
                var length = Math.Min(64, base64.Length - i);
                sb.AppendLine(base64.Substring(i, length));
            }

            sb.AppendLine("-----END CERTIFICATE-----");

            File.WriteAllText(path, sb.ToString(), Encoding.ASCII);
        }

        private static X509Certificate2 CreateCaCertificate()
        {
            using var rsa = RSA.Create(4096);

            var subject = new X500DistinguishedName($"CN={CaName}, O={Constants.AppName}, OU={OuName}");

            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: true,
                pathLengthConstraint: 0, critical: true));

            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));

            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(10));
        }

        private static X509Certificate2 CreateServerCertificate(X509Certificate2 ca, string ipAddress)
        {
            using var rsa = RSA.Create(2048);

            var subject = new X500DistinguishedName($"CN={ipAddress}, O={Constants.AppName}, OU={OuName}");

            var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, 
                pathLengthConstraint: 0, critical: true));

            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));

            var eku = new OidCollection
            {
                new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication")
            };

            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, critical: true));

            var sanBuilder = new SubjectAlternativeNameBuilder();

            sanBuilder.AddDnsName("localhost");

            if (IPAddress.TryParse(ipAddress, out var ip))
            {
                sanBuilder.AddIpAddress(ip);
            }
            else
            {
                sanBuilder.AddDnsName(ipAddress);
            }

            request.CertificateExtensions.Add(sanBuilder.Build());

            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

            var serialNumber = RandomNumberGenerator.GetBytes(16);

            var certificate = request.Create(ca, DateTimeOffset.UtcNow.AddMinutes(-5), 
                DateTimeOffset.UtcNow.AddYears(2), serialNumber);
            var serverCertificate = certificate.CopyWithPrivateKey(rsa);
            serverCertificate.FriendlyName = ScName;
        
            return serverCertificate;
        }

        private static void InstallCaCertificate(X509Certificate2 ca)
        {
            using var publicCertificate = X509CertificateLoader.LoadCertificate(ca.Export(X509ContentType.Cert));

            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadWrite);

            Logger.Information("Checking installed CA certificates for {Subject}. Current thumbprint: {Thumbprint}",
                ca.Subject, ca.Thumbprint);

            var existing = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName,
                ca.Subject, validOnly: false);

            var currentInstalled = false;

            foreach (var certificate in existing)
            {
                if (string.Equals(certificate.Thumbprint, ca.Thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Information("Current CA certificate is already installed. Thumbprint: {Thumbprint}", certificate.Thumbprint);

                    currentInstalled = true;
                }
                else
                {
                    Logger.Information("Removing old CA certificate. Thumbprint: {Thumbprint}", certificate.Thumbprint);

                    store.Remove(certificate);
                }

                certificate.Dispose();
            }

            if (currentInstalled)
            {
                return;
            }

            Logger.Information("Current CA certificate is not installed. Installing it. Thumbprint: {Thumbprint}", ca.Thumbprint);

            store.Add(publicCertificate);

            Logger.Information("Successfully installed CA certificate. Thumbprint: {Thumbprint}", ca.Thumbprint);
        }
        
        private static bool CertificateMatchesIp(X509Certificate2 certificate, string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out var expectedIp))
            {
                return false;
            }

            foreach (var extension in certificate.Extensions)
            {
                if (extension is not X509SubjectAlternativeNameExtension san)
                    continue;

                foreach (var entry in san.EnumerateDnsNames())
                {
                    if (string.Equals(entry, ipAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                foreach (var entry in san.EnumerateIPAddresses())
                {
                    if (entry.Equals(expectedIp))
                        return true;
                }
            }

            return false;
        }

        private static X509Certificate2 LoadCertificate(string path)
        {
            return X509CertificateLoader.LoadPkcs12FromFile(path, password: null, keyStorageFlags: X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
        }
    }
}