using System.Security.Cryptography;

namespace FarmServer
{
    public static class SecurityUtils
    {
        public static string GenerateSessionId()
        {
            // 32 bytes = 256 bits of entropy
            byte[] bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            // URL-safe base64
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }
    }
}
