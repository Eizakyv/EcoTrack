using System.Security.Cryptography;
using System.Text;

namespace EcoTrack.Helpers
{
    // Provides cryptographic helper methods for the application.
    public static class SecurityHelper
    {
        // Computes the SHA‑256 hash of the input string.
        // Returns the hash as a lowercase hexadecimal string.
        // Throws ArgumentNullException if rawData is null.
        public static string ComputeSha256Hash(string rawData)
        {
            // Convert the input string to a UTF‑8 byte array.
            byte[] inputBytes = Encoding.UTF8.GetBytes(rawData);

            // Compute the SHA‑256 hash of the byte array.
            byte[] hashBytes = SHA256.HashData(inputBytes);

            // Convert the hash bytes to a lowercase hexadecimal string.
            return Convert.ToHexStringLower(hashBytes);
        }
    }
}