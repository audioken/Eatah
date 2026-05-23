namespace Eatah.Api.Features.Push;

public class PushSettings
{
    /// <summary>VAPID subject – a mailto: or https: URI identifying the sender.</summary>
    public string Subject { get; set; } = "mailto:noreply@eatah.app";
    /// <summary>VAPID EC P-256 public key in base64url encoding (65-byte uncompressed point).</summary>
    public string? PublicKey { get; set; }
    /// <summary>VAPID EC P-256 private key in base64url encoding (32-byte scalar).</summary>
    public string? PrivateKey { get; set; }
}
