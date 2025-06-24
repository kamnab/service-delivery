public class Client
{
    public static bool Validate(string key)
    {
        var publicKey = File.ReadAllBytes("Infrastructure/Resources/public.key");
        var aesKey = File.ReadAllBytes("Infrastructure/Resources/aes.key");
        var aesIV = File.ReadAllBytes("Infrastructure/Resources/aes.iv");

        var rsa = CryptoUtils.LoadRsaPublicKey(publicKey);
        if (LicenseManager.TryValidateActivationKey(key, rsa, aesKey, aesIV, out var license, out var error))
        {
            Console.WriteLine($"✅ Activated: {license!.Edition}, expires {license!.Expiration} on Machine Id: {license!.MachineId}");

            return true;
        }
        else
        {
            Console.WriteLine($"❌ Invalid license: {error}");
        }
        return false;
    }
}

