using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System.IO;
using System.Threading.Tasks;

// namespace Gml.Web.Api.Core.Services
// {
//     public class SystemService : ISystemService
//     {
//         private static readonly string _publicKeyPath = Path.Combine(AppContext.BaseDirectory, "public.pem");
//         private static readonly string _privateKeyPath = Path.Combine(AppContext.BaseDirectory, "private.pem");
//
//         public async Task<string> GetPublicKey()
//         {
//             return await ReadKeyFile(_publicKeyPath);
//         }
//
//         public async Task<string> GetPrivateKey()
//         {
//             return await ReadKeyFile(_privateKeyPath);
//         }
//
//         public async Task<string> GetSignature(string data)
//         {
//             var privateKey = await GetPrivateKey();
//             var rsa = RSA.Create();
//             rsa.ImportFromPem(privateKey.ToCharArray());
//
//             var inputBytes = Encoding.UTF8.GetBytes(data);
//             var signatureBytes = rsa.SignData(inputBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
//
//             return Convert.ToBase64String(signatureBytes);
//         }
//
//         public async Task<string> GetBase64FromImageFile(MemoryStream ms)
//         {
//             var fileBytes = ms.ToArray();
//             var base64 = Convert.ToBase64String(fileBytes);
//
//             return base64;
//         }
//
//         private async Task<string> ReadKeyFile(string path)
//         {
//             if (!File.Exists(path)) GenerateKeyPair();
//
//             using var reader = new StreamReader(path);
//             return await reader.ReadToEndAsync();
//         }
//
//         private void GenerateKeyPair(int keySize = 4096)
//         {
//             var randomGenerator = new CryptoApiRandomGenerator();
//             var secureRandom = new SecureRandom(randomGenerator);
//             var keyPairGen = new RsaKeyPairGenerator();
//             keyPairGen.Init(new KeyGenerationParameters(secureRandom, keySize));
//             var keyPair = keyPairGen.GenerateKeyPair();
//
//             ExportKeyPair(_privateKeyPath, _publicKeyPath, keyPair);
//         }
//
//         private static void ExportKeyPair(string privateKeyFile, string publicKeyFile, AsymmetricCipherKeyPair keyPair)
//         {
//             using (var textWriter = new StreamWriter(privateKeyFile))
//             {
//                 var pemWriter = new PemWriter(textWriter);
//                 // Export private key in PKCS#8 format
//                 pemWriter.WriteObject(keyPair.Private);
//             }
//
//             using (var textWriter = new StreamWriter(publicKeyFile))
//             {
//                 var pemWriter = new PemWriter(textWriter);
//                 // Export public key in X.509 SPKI format
//                 pemWriter.WriteObject(keyPair.Public);
//             }
//         }
//     }
// }

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

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
        var signatureBytes = csp.SignData(inputBytes, "SHA1");

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

    private void GenerateKeyPair()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "openssl",
            Arguments = $"genrsa -out {privateKeyPath} 4096",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = new FileInfo(privateKeyPath).Directory!.FullName
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();

        startInfo.Arguments = $"rsa -in {privateKeyPath} -out {publicKeyPath} -pubout";

        var processSecondCommand = new Process { StartInfo = startInfo }; // Create new Process instance
        processSecondCommand.Start();
        processSecondCommand.WaitForExit();
    }
}
