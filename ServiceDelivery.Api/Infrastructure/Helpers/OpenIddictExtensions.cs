using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.ConstrainedExecution;

namespace Talkemie.Authentication.Infrastructure.Helpers
{
    public class OpenIddictHelpers
    {
        public static X509Certificate2 LoadEncryptionCertificate()
        {
            string filename = "encryption-certificate.pfx";
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Resources", filename);

            //string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Resources");
            //var di = new DirectoryInfo(folderPath);
            var fi = 
                //di.GetFiles().FirstOrDefault();
                new FileInfo(path);
            if (fi.Exists)
            {
                return new X509Certificate2(File.ReadAllBytes(fi.FullName));
            }

            return GetEncryptionCertificate();
        }

        public static X509Certificate2 LoadSigningCertificate()
        {
            var filename = "signing-certificate.pfx";
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Resources", filename);
            var fi = new FileInfo(path);
            if (fi.Exists)
            {
                return new X509Certificate2(File.ReadAllBytes(fi.FullName));
            }

            //string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Resources");
            //var di = new DirectoryInfo(folderPath);
            //var fi = di.GetFiles().FirstOrDefault();
            //if (fi != null)
            //{
            //    return new X509Certificate2(File.ReadAllBytes(fi.FullName));
            //}

            return GetSigningCertificate();
        }

        public static X509Certificate2 GetEncryptionCertificate()
        {
            using var algorithm = RSA.Create(keySizeInBits: 2048);

            var subject = new X500DistinguishedName("CN=Talkemie Authentication Encryption Certificate");
            var request = new CertificateRequest(subject, algorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, critical: true));

            var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(20));
            var certificate = new X509Certificate2(cert.Export(X509ContentType.Pfx, string.Empty), string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            string filename = "encryption-certificate.pfx";
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Resources", filename);

            Console.WriteLine(path);
            File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, string.Empty));

            return certificate;
        }

        public static X509Certificate2 GetSigningCertificate()
        {
            using var algorithm = RSA.Create(keySizeInBits: 2048);

            var subject = new X500DistinguishedName("CN=Talkemie Authentication Signing Certificate");
            var request = new CertificateRequest(subject, algorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

            var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(20));
            var certificate = new X509Certificate2(cert.Export(X509ContentType.Pfx, string.Empty), string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            var filename = "signing-certificate.pfx";
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Resources", filename);
            Console.WriteLine(path);

            File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, string.Empty));
            return certificate;
        }

    }
}
