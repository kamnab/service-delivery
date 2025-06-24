using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static class LicenseManager
{
    public static string GenerateActivationKey(
        LicenseInfo license, RSA rsaPrivateKey, byte[] aesKey, byte[] aesIV)
    {
        var json = JsonSerializer.Serialize(license);
        var plainBytes = Encoding.UTF8.GetBytes(json);

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = aesIV;
        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var signature = rsaPrivateKey.SignData(encrypted, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var buffer = new byte[signature.Length + encrypted.Length];
        Buffer.BlockCopy(signature, 0, buffer, 0, signature.Length);
        Buffer.BlockCopy(encrypted, 0, buffer, signature.Length, encrypted.Length);

        return Convert.ToBase64String(buffer);
    }

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