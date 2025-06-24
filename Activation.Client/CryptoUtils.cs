// CryptoUtils.cs (partial)
using System.Net.NetworkInformation;
using System.Security.Cryptography;

public static class CryptoUtils
{
    public static string GetMachineId()
    {
        var mac = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            ?.GetPhysicalAddress()?.ToString();

        return mac ?? "unknown";
    }

    public static RSA LoadRsaPublicKey(byte[] publicKey)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(publicKey, out _);
        return rsa;
    }
}