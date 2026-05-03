using System.Security.Cryptography;
using System.Text;

namespace TrueAltitude.API.Security;

public static class RequestTimeHeaderValidator
{
    public const string HeaderName = "X-Request-Time";

    public static bool IsValid(string? headerValue, string sharedKey, int maxAgeSeconds)
    {
        if (string.IsNullOrWhiteSpace(headerValue) || string.IsNullOrWhiteSpace(sharedKey))
        {
            return false;
        }

        var parts = headerValue.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var iv = Convert.FromBase64String(parts[0]);
            var cipherWithTag = Convert.FromBase64String(parts[1]);

            if (iv.Length != 12 || cipherWithTag.Length <= 16)
            {
                return false;
            }

            var cipher = cipherWithTag[..^16];
            var tag = cipherWithTag[^16..];

            using var sha256 = SHA256.Create();
            var key = sha256.ComputeHash(Encoding.UTF8.GetBytes(sharedKey));

            var plaintext = new byte[cipher.Length];
            using var aesGcm = new AesGcm(key, 16);
            aesGcm.Decrypt(iv, cipher, tag, plaintext);

            var timestampText = Encoding.UTF8.GetString(plaintext);
            if (!long.TryParse(timestampText, out var sentUnixMs))
            {
                return false;
            }

            var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var skew = Math.Abs(nowUnixMs - sentUnixMs);
            return skew <= maxAgeSeconds * 1000L;
        }
        catch
        {
            return false;
        }
    }
}