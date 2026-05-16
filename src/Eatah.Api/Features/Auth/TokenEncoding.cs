using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Eatah.Api.Features.Auth;

/// <summary>
/// Token encoding helpers. Identity tokens contain characters that don't survive URL transport
/// without Base64Url-encoding.
/// </summary>
public static class TokenEncoding
{
    public static string Encode(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    public static bool TryDecode(string encoded, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
            return true;
        }
        catch
        {
            token = string.Empty;
            return false;
        }
    }
}
