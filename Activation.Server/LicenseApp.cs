using System.Security.Cryptography;
public class LicenseApp
{
    const string FOLDER = "Infrastructure/Resources/";

    public static string GenerateKey(LicenseInfo license)
    {
        // Load private key & AES config
        var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(File.ReadAllBytes(FOLDER + "private.key"), out _);
        var aesKey = File.ReadAllBytes(FOLDER + "aes.key");
        var aesIV = File.ReadAllBytes(FOLDER + "aes.iv");

        var key = LicenseManager.GenerateActivationKey(license, rsa, aesKey, aesIV);
        Console.WriteLine("Activation Key:\n" + key);

        return key;
    }

    public static bool Validate(string key, out LicenseInfo license)
    {
        var publicKey = File.ReadAllBytes(FOLDER + "public.key");
        var aesKey = File.ReadAllBytes(FOLDER + "aes.key");
        var aesIV = File.ReadAllBytes(FOLDER + "aes.iv");

        var rsa = CryptoUtils.LoadRsaPublicKey(publicKey);
        if (LicenseManager.TryValidateActivationKey(key, rsa, aesKey, aesIV, out license, out var error))
        {
            Console.WriteLine($"✅ Activated: {license.Edition}, expires {license.Expiration}");

            return true;
        }
        else
        {
            Console.WriteLine($"❌ Invalid license: {error}");
        }
        return false;
    }

}