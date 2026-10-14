using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Gml.Domains.Repositories;
using Gml.Domains.User;
using Gml.Dto.Messages;
using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using GmlCore.Interfaces.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Gml.WebApi.Tests;

[NonParallelizable]
public class BuildInfoTests
{
    private const string SecurityKey = "build-info-tests-signing-key-at-least-32-bytes";
    private const string ExternalAppToken = "valid-external-application-key";
    private const string VersionPath = "/api/v1/system/version";
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private string _directory = null!;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"gml-build-info-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);

        // Use the production service/authentication registration, without starting
        // Program's database migrations, plugin loading or launcher initialization.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(BuildInfo).Assembly.GetName().Name,
            ContentRootPath = _directory,
            EnvironmentName = "Testing"
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SQLite"] = $"Data Source={Path.Combine(_directory, "test.db")}"
        });

        var environment = new Dictionary<string, string>
        {
            ["SECURITY_KEY"] = SecurityKey,
            ["PROJECT_NAME"] = "BuildInfoTests",
            ["PROJECT_POLICYNAME"] = "BuildInfoTests",
            ["MARKET_ENDPOINT"] = "https://example.invalid",
            ["PROJECT_PATH"] = _directory,
            ["SERVICE_TEXTURE_ENDPOINT"] = "https://example.invalid",
            ["JWT_ISSUER"] = "gml-api",
            ["JWT_AUDIENCE"] = "gml-clients"
        };
        var previousEnvironment = environment.Keys.ToDictionary(key => key, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var (key, value) in environment)
                Environment.SetEnvironmentVariable(key, value);

            builder.RegisterServices();
        }
        finally
        {
            foreach (var (key, value) in previousEnvironment)
                Environment.SetEnvironmentVariable(key, value);
        }

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.RegisterEndpoints();
        // Prove that the application key is valid under the production default
        // scheme before asserting it cannot access the JWT-only version route.
        _app.MapGet("/test/application-auth", (HttpContext context) =>
                Results.Ok(context.User.HasClaim(claim => claim.Type == "app_id")))
            .RequireAuthorization();

        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await db.Database.EnsureCreatedAsync();
            var user = new DbUser
            {
                Login = "build-info-tests",
                Email = "build-info@example.invalid",
                Password = "unused",
                AccessToken = "unused"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();
            var repository = scope.ServiceProvider.GetRequiredService<IExternalApplicationRepository>();
            await repository.CreateAsync(user.Id, "Build info test", tokenService.HashRefreshToken(ExternalAppToken), []);
        }

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        _client?.Dispose();
        if (_app != null)
            await _app.DisposeAsync();
        if (_directory != null)
            Directory.Delete(_directory, recursive: true);
    }

    [TestCase(null)]
    [TestCase("Admin")]
    public async Task PanelJwtReturnsEmbeddedBuildInformation(string? role)
    {
        using var response = await GetVersion(CreateJwt(role: role));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadFromJsonAsync<ResponseMessage<BuildInfo>>();
        var metadata = typeof(BuildInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);

        Assert.Multiple(() =>
        {
            Assert.That(body?.StatusCode, Is.EqualTo(200));
            Assert.That(body?.Data?.Version, Is.EqualTo(metadata["BuildVersion"]));
            Assert.That(body?.Data?.Commit, Is.EqualTo(metadata["BuildCommit"]));
            Assert.That(response.Headers.CacheControl?.NoStore, Is.True);
        });
    }

    [TestCase(null)]
    [TestCase("invalid-token")]
    [TestCase("eyJinvalid.jwt.token")]
    public async Task MissingOrInvalidTokenIsRejected(string? token)
    {
        using var response = await GetVersion(token);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ExpiredJwtIsRejected()
    {
        using var response = await GetVersion(CreateJwt(expires: DateTime.UtcNow.AddMinutes(-1)));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase("wrong-issuer", "gml-clients", SecurityKey)]
    [TestCase("gml-api", "wrong-audience", SecurityKey)]
    [TestCase("gml-api", "gml-clients", "another-signing-key-at-least-32-bytes")]
    public async Task JwtWithInvalidIssuerAudienceOrSignatureIsRejected(string issuer, string audience, string key)
    {
        using var response = await GetVersion(CreateJwt(issuer: issuer, audience: audience, key: key));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ValidExternalApplicationKeyIsRejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/application-auth");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ExternalAppToken);
        using var proof = await _client.SendAsync(request);
        Assert.That(proof.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await proof.Content.ReadFromJsonAsync<bool>(), Is.True);

        using var response = await GetVersion(ExternalAppToken);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private async Task<HttpResponseMessage> GetVersion(string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, VersionPath);
        if (token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private static string CreateJwt(string? role = null, DateTime? expires = null,
        string issuer = "gml-api", string audience = "gml-clients", string key = SecurityKey)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        if (role != null)
            claims.Add(new Claim(ClaimTypes.Role, role));
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: expires ?? DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
