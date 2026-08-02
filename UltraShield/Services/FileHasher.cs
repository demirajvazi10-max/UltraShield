using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace UltraShield.Services
{
    public static class FileHasher
    {
        public static async Task<string> ComputeSha256Async(string filePath)
        {
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
