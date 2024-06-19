using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Gml.Web.Api.Core.Services;

public class SystemService : ISystemService
{
    private static readonly string publicKeyPath = "public.key";
    private static readonly string privateKeyPath = "private.key";

    public async Task<string> GetPublicKey()
    {
        return await ReadKeyFile(publicKeyPath);
    }

    public async Task<string> GetPrivateKey()
    {
        return await ReadKeyFile(privateKeyPath);
    }

    public async Task<string> GetSignature(string data)
    {
        var privateKey = await GetPrivateKey();

        var csp = new RSACryptoServiceProvider(4096);
        csp.ImportFromPem(privateKey);

        var inputBytes = Encoding.UTF8.GetBytes(data);
        var signatureBytes = csp.SignData(inputBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

        return Convert.ToBase64String(signatureBytes);
    }

    public async Task<string> GetBase64FromImageFile(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileBytes = ms.ToArray();
        var base64 = Convert.ToBase64String(fileBytes);

        return base64;
    }

    private async Task<string> ReadKeyFile(string path)
    {
        if (!File.Exists(path)) GenerateKeyPair();

        using var reader = new StreamReader(path);

        return await reader.ReadToEndAsync();
    }

    private void GenerateKeyPair(int keySize = 4096)
    {
        CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
        SecureRandom secureRandom = new SecureRandom(randomGenerator);
        RsaKeyPairGenerator keyPairGen = new RsaKeyPairGenerator();
        keyPairGen.Init(new KeyGenerationParameters(secureRandom, keySize));
        AsymmetricCipherKeyPair keyPair = keyPairGen.GenerateKeyPair();

        // Catch any exceptions when trying to write keys to files
        try
        {
            ExportKeyPair(privateKeyPath, publicKeyPath, keyPair);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while trying to write keys to files: {ex.Message}");
        }
    }

    private static void ExportKeyPair(string privateKeyFile, string publicKeyFile, AsymmetricCipherKeyPair keyPair)
    {
        try
        {
            using (TextWriter textWriter = new StreamWriter(privateKeyFile))
            {
                PemWriter pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(keyPair.Private);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to private key file: {ex.Message}");
            throw;
        }

        try
        {
            using (TextWriter textWriter = new StreamWriter(publicKeyFile))
            {
                PemWriter pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(keyPair.Public);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to public key file: {ex.Message}");
            throw;
        }
    }
}
