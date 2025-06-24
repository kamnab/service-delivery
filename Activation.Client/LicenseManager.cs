// LicenseManager.cs (Validate only)
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class LicenseManager
{
    public static bool TryValidateActivationKey(
        string activationKey, RSA rsaPublicKey, byte[] aesKey, byte[] aesIV,
        out LicenseInfo? license, out string? error)
    {
        license = null;
        error = null;

        try
        {
            var buffer = Convert.FromBase64String(activationKey);
            int sigLen = rsaPublicKey.KeySize / 8;

            var signature = new byte[sigLen];
            var encrypted = new byte[buffer.Length - sigLen];
            Buffer.BlockCopy(buffer, 0, signature, 0, sigLen);
            Buffer.BlockCopy(buffer, sigLen, encrypted, 0, encrypted.Length);

            if (!rsaPublicKey.VerifyData(encrypted, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                error = "Invalid signature.";
                return false;
            }

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = aesIV;
            using var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

            var json = Encoding.UTF8.GetString(decrypted);
            license = JsonSerializer.Deserialize<LicenseInfo>(json);

            if (license == null)
            {
                error = "Failed to parse license.";
                return false;
            }

            if (license.MachineId != CryptoUtils.GetMachineId())
            {
                error = "License not valid for this machine.";
                return false;
            }

            if (DateTime.UtcNow > license.Expiration)
            {
                error = "License expired.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"Validation failed: {ex.Message}";
            return false;
        }
    }
}