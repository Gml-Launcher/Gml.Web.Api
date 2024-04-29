using System.Security.Cryptography;

namespace Gml.Web.Api.Core.Services;

public class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 20;

    public static string Hash(string password)
    {
        using var algorithm = new Rfc2898DeriveBytes(password, SaltSize, 10000, HashAlgorithmName.SHA256);

        var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
        var salt = Convert.ToBase64String(algorithm.Salt);

        return $"{salt}.{key}";
    }

    public static bool Verify(string hash, string password)
    {
        var parts = hash.Split('.', 2);

        if (parts.Length != 2)
            throw new FormatException("Unexpected hash format. Should be formatted as `{salt}.{key}`");

        var salt = Convert.FromBase64String(parts[0]);
        var key = Convert.FromBase64String(parts[1]);

        using var algorithm = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        var keyToCheck = algorithm.GetBytes(KeySize);

        return keyToCheck.Length == key.Length && Compare(key, keyToCheck);
    }

    private static bool Compare(byte[] array1, byte[] array2)
    {
        if (array1.Length != array2.Length) return false;

        return !array1.Where((t, i) => t != array2[i]).Any();
    }
}
