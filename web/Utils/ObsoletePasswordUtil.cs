using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace PhiZoneApi.Utils;

public static class ObsoletePasswordUtil
{
    public static bool Check(string password, string storage)
    {
        var parts = storage.Split('$');
        if (parts is not ["pbkdf2_sha256", _, _, _]) return false;

        var iterations = int.Parse(parts[1]);
        var salt = Encoding.UTF8.GetBytes(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);

        var actualHash = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            iterations,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}