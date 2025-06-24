public static class LicenseKeyGeneration
{
    public static void Run()
    {
        // RSA 2048
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var privKey = rsa.ExportRSAPrivateKey();
        var pubKey = rsa.ExportRSAPublicKey();
        File.WriteAllBytes("private.key", privKey);
        File.WriteAllBytes("public.key", pubKey);

        // AES 256
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        aes.GenerateIV();
        File.WriteAllBytes("aes.key", aes.Key);
        File.WriteAllBytes("aes.iv", aes.IV);
    }
}