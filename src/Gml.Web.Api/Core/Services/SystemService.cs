using System.Security.Cryptography;
using System.Text;

namespace Gml.Web.Api.Core.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

public class SystemService : ISystemService
{
    private static string publicKeyPath = "public.key";
    private static string privateKeyPath = "private.key";

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

        byte[] inputBytes = Encoding.UTF8.GetBytes(data);
        byte[] signatureBytes = csp.SignData(inputBytes, "SHA1");
        return Convert.ToBase64String(signatureBytes);
    }

    private async Task<string> ReadKeyFile(string path)
    {
        if (!File.Exists(path))
        {
            GenerateKeyPair();
        }

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
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();

        startInfo.Arguments = $"rsa -in {privateKeyPath} -out {publicKeyPath} -pubout";
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
    }
}