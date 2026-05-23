namespace Eatah.Domain.Entities;

public class PushSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Push endpoint URL provided by the browser.</summary>
    public string Endpoint { get; set; } = "";
    /// <summary>Browser-generated P-256 public key (base64url).</summary>
    public string P256dh { get; set; } = "";
    /// <summary>Browser-generated auth secret (base64url).</summary>
    public string Auth { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
