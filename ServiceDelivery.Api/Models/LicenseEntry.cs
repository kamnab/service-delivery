using System.ComponentModel.DataAnnotations;

public class LicenseEntry
{
    public Guid Id { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
