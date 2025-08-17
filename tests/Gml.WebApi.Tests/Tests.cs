using System.Diagnostics;
using System.Net;
using Faker;
using Gml.Models.Auth;
using Gml.Web.Api.Domains.LauncherDto;
using Gml.Web.Api.Domains.System;
using Gml.Web.Api.Dto.Files;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.Launcher;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Minecraft.AuthLib;
using Gml.Web.Api.Dto.Player;
using Gml.Web.Api.Dto.Profile;
using Gml.Web.Api.Dto.Settings;
using Gml.Web.Api.Dto.Texture;
using Gml.Web.Api.Dto.User;
using GmlCore.Interfaces.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;

namespace Gml.WebApi.Tests;

public class Tests
{
    private readonly string _newSentryUrl = "https://gml@gmlb-test.recloud.tech/1";
    private readonly string _newTextureUrl = "https://test.ru";
    private readonly string _profileName = "UnitTestProfile";
    private string? _cloakUrl;
    private HttpClient _httpClient;
    private readonly string _serverUuid = Guid.NewGuid().ToString();
    private string? _skinUrl;
    private WebApplicationFactory<Program> _webApplicationFactory;
    private string? _accessToken;

    [OneTimeSetUp]
    public void Setup()
    {
        try
        {
            Environment.SetEnvironmentVariable("SECURITY_KEY", "jkuhbsfgvuk4gfikhn8i7wa34rkbqw23");
            Environment.SetEnvironmentVariable("PROJECT_NAME", "GmlServer");
            Environment.SetEnvironmentVariable("MARKET_ENDPOINT", "https://gml-market.recloud.tech");
            Environment.SetEnvironmentVariable("PROJECT_DESCRIPTION", "GmlServer Description");
            Environment.SetEnvironmentVariable("PROJECT_POLICYNAME", "GmlPolicy");
            Environment.SetEnvironmentVariable("PROJECT_PATH", "");
            Environment.SetEnvironmentVariable("SERVICE_TEXTURE_ENDPOINT", "http://gml-web-skins:8085");

            _webApplicationFactory = new GmlApiApplicationFactory();
            _httpClient = _webApplicationFactory.CreateClient();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    [Order(1)]
    public async Task RemoveAllProfilesEndFiles()
    {
        var response = await _httpClient.GetAsync("/api/v1/profiles");

        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<List<ProfileReadDto>>>(content);

        foreach (var profile in model?.Data ?? Enumerable.Empty<ProfileReadDto>())
        {
            var deleteResponse = await _httpClient.DeleteAsync($"/api/v1/profiles/{profile.Name}?removeFiles=true");

            Assert.That(deleteResponse.IsSuccessStatusCode, Is.True);
        }

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.That(response.IsSuccessStatusCode, Is.True);
        });
    }

    [Test]
    [Order(2)]
    public async Task CreateProfile()
    {
        var profile = new MultipartFormDataContent();
        profile.Add(new StringContent(_profileName), "Name");
        profile.Add(new StringContent(_profileName), "DisplayName");
        profile.Add(new StringContent(Address.StreetAddress()), "Description");
        profile.Add(new StringContent("1.7.10"), "Version");
        profile.Add(new StringContent(((int)GameLoader.Forge).ToString()), "GameLoader");
        profile.Add(new StringContent("10.13.4.1614"), "LoaderVersion");

        var response = await _httpClient.PostAsync("/api/v1/profiles", profile);

        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<ProfileReadDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(model, Is.Not.Null);
            Assert.That(model?.Data?.Name, Is.EqualTo(_profileName));
        });
    }

#if DEBUG
    [Test]
    [Order(3)]
    public async Task RestoreProfile()
    {
        var restoreDto = TestHelper.CreateJsonObject(new ProfileRestoreDto
        {
            Name = _profileName
        });

        var response = await _httpClient.PostAsync("/api/v1/profiles/restore", restoreDto);

        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }
#endif

    [Test]
    [Order(4)]
    public async Task GetProfileInfo()
    {
        try
        {
            var profile = TestHelper.CreateJsonObject(new ProfileCreateInfoDto
            {
                ProfileName = _profileName,
                GameAddress = "localhost",
                GamePort = 25565,
                RamSize = 4096,
                WindowWidth = 900,
                WindowHeight = 600,
                IsFullScreen = true,
                OsType = ((int)OsType.Windows).ToString(),
                OsArchitecture = Environment.Is64BitOperatingSystem ? "64" : "32",
                UserAccessToken = "accessToken",
                UserName = "GamerVII",
                UserUuid = "userUuid"
            });

            var response = await _httpClient.PostAsync("/api/v1/profiles/details", profile);
            var content = await response.Content.ReadAsStringAsync();

            var model = JsonConvert.DeserializeObject<ResponseMessage<ProfileReadInfoDto>>(content);

            Assert.Multiple(() =>
            {
                Assert.That(model, Is.Not.Null);
                Assert.IsTrue(response.IsSuccessStatusCode);
                Assert.That(model?.Data?.ProfileName, Is.Not.Empty);
                Assert.That(model?.Data?.ProfileName, Is.EqualTo(_profileName));
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Debug.WriteLine(e);
            throw;
        }
    }

    [Test]
    [Order(5)]
    public async Task UpdateProfile()
    {
        var profileUpdateData = new MultipartFormDataContent
        {
            { new StringContent(_profileName), "Name" },
            { new StringContent(_profileName), "DisplayName" },
            { new StringContent(_profileName), "OriginalName" },
            { new StringContent("Avon"), "Description" },
            { new StringContent("image"), "IconBase64" }
        };

        var response = await _httpClient.PutAsync("/api/v1/profiles", profileUpdateData);

        var content = await response.Content.ReadAsStringAsync();
        var model = JsonConvert.DeserializeObject<ResponseMessage<SettingsReadDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.IsTrue(response.IsSuccessStatusCode);
        });
    }

    [Test]
    [Order(6)]
    public async Task CompileProfile()
    {
        var profile = TestHelper.CreateJsonObject(new ProfileCompileDto
        {
            Name = _profileName
        });

        var response = await _httpClient.PostAsync("/api/v1/profiles/compile", profile);
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<ProfileReadInfoDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.IsTrue(response.IsSuccessStatusCode);
        });
    }

    [Test]
    [Order(7)]
    public async Task GetSettings()
    {
        var response = await _httpClient.GetAsync("/api/v1/settings/platform");
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<SettingsReadDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.IsTrue(response.IsSuccessStatusCode);
        });
    }

    [Test]
    [Order(8)]
    public async Task UpdateSettings()
    {
        var httpContent = TestHelper.CreateJsonObject(new SettingsUpdateDto
        {
            RegistrationIsEnabled = true
        });

        var response = await _httpClient.PutAsync("/api/v1/settings/platform", httpContent);
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<SettingsReadDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.IsTrue(response.IsSuccessStatusCode);
        });
    }

    [Test]
    [Order(9)]
    public async Task UpdateSkinsUrl()
    {
        var httpContent = TestHelper.CreateJsonObject(new UrlServiceDto(_newTextureUrl));

        var response = await _httpClient.PutAsync("/api/v1/integrations/texture/skins", httpContent);
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<UrlServiceDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.IsTrue(response.IsSuccessStatusCode);
        });
    }

    [Test]
    [Order(10)]
    public async Task UpdateCloaksUrl()
    {
        var httpContent = TestHelper.CreateJsonObject(new UrlServiceDto(_newTextureUrl));

        var response = await _httpClient.PutAsync("/api/v1/integrations/texture/cloaks", httpContent);
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<UrlServiceDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.That(response.IsSuccessStatusCode, Is.True);
        });
    }

    [Test]
    [Order(11)]
    public async Task GetSkinsUrl()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/texture/skins");
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<UrlServiceDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.That(model?.Data, Is.Not.Null);
            Assert.That(model?.Data?.Url, Is.EqualTo(_newTextureUrl));
            Assert.That(response.IsSuccessStatusCode, Is.True);
        });
    }

    [Test]
    [Order(12)]
    public async Task GetCloaksUrl()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/texture/cloaks");
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<UrlServiceDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.That(model?.Data, Is.Not.Null);
            Assert.That(model?.Data?.Url, Is.EqualTo(_newTextureUrl));
            Assert.That(response.IsSuccessStatusCode, Is.True);
        });
    }

    [Test]
    [Order(13)]
    public async Task UpdateSentryDsn()
    {
        var httpContent = TestHelper.CreateJsonObject(new UrlServiceDto(_newSentryUrl));

        var response = await _httpClient.PutAsync("/api/v1/integrations/sentry/dsn", httpContent);
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<UrlServiceDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.That(response.IsSuccessStatusCode, Is.True);
        });
    }

    [Test]
    [Order(14)]
    public async Task GetSentryDsn()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/sentry/dsn");
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<UrlServiceDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.That(model?.Data, Is.Not.Null);
            Assert.That(model?.Data?.Url, Is.EqualTo(_newSentryUrl));
            Assert.That(response.IsSuccessStatusCode, Is.True);
        });
    }

    [Test]
    [Order(15)]
    public async Task GetLauncherVersions()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/github/launcher/versions");
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<List<LauncherVersionReadDto>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.That(model?.Data, Is.Not.Null);
            Assert.That(model?.Data?.Any(), Is.True);
            Assert.That(response.IsSuccessStatusCode, Is.True);
        });
    }

    [Test]
    [Order(16)]
    public async Task CompileLauncherVersions()
    {
        var httpContent = TestHelper.CreateJsonObject(new LauncherCreateDto
        {
            GitHubVersions = "v0.1.0-beta1",
            Host = "https://localhost:5000",
            Folder = "GmlLauncher"
        });

        var response = await _httpClient.PostAsync("/api/v1/integrations/github/launcher/download", httpContent);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            // Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(string.IsNullOrWhiteSpace(content), Is.False);
        });
    }

    [Test]
    [Order(17)]
    public async Task DownloadLauncherVersions()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/github/launcher/download/v0.1.0-beta1");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(string.IsNullOrWhiteSpace(content), Is.False);
            Assert.That(response.IsSuccessStatusCode, Is.True);
        });
    }

    [Test]
    [Order(18)]
    public async Task SetAuthService()
    {
        var httpContent = TestHelper.CreateJsonObject(new AuthServiceInfo("Any", AuthType.Any)
        {
            Endpoint = "http://test.ru/endpoint"
        });

        var response = await _httpClient.PutAsync("/api/v1/integrations/auth", httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(19)]
    public async Task Auth()
    {
        var result = await Auth("GamerVII", "MegaPassword");

        _accessToken = result.User?.Data?.AccessToken;

        Assert.Multiple(() => { Assert.That(result.IsSuccess, Is.True); });
    }

    private async Task<(ResponseMessage<PlayerReadDto>? User, bool IsSuccess)> Auth(string login, string password)
    {
        var httpContent = TestHelper.CreateJsonObject(new UserAuthDto
        {
            Login = login,
            Password = password
        });

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"Gml.Launcher-Client-GmlClientManager/1.0 (OS: {Environment.OSVersion};)");

        var response = await _httpClient.PostAsync("/api/v1/integrations/auth/signin", httpContent);

        var content = await response.Content.ReadAsStringAsync();

        return (JsonConvert.DeserializeObject<ResponseMessage<PlayerReadDto>>(content), response.IsSuccessStatusCode);
    }

    [Test]
    [Order(20)]
    public async Task UpdateDiscordInfo()
    {
        var httpContent = TestHelper.CreateJsonObject(new DiscordRpcUpdateDto
        {
            ClientId = "1122236654898744156",
            Details = "Description",
            LargeImageKey = "logo",
            LargeImageText = "logo",
            SmallImageKey = "logo",
            SmallImageText = "logo"
        });

        var response = await _httpClient.PutAsync("/api/v1/integrations/discord", httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(21)]
    public async Task GetDiscordInfo()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/discord");

        var content = await response.Content.ReadAsStringAsync();

        var data = JsonConvert.DeserializeObject<ResponseMessage<DiscordRpcReadDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(data?.Data, Is.Not.Null);
            Assert.That(data?.Data?.Details, Is.Not.Empty);
        });
    }

    [Test]
    [Order(22)]
    public async Task SetSkinUrl()
    {
        var httpContent = TestHelper.CreateJsonObject(new UrlServiceDto
        {
            Url = _skinUrl = "https://example.com/skin.png"
        });

        var response = await _httpClient.PutAsync("/api/v1/integrations/texture/skins", httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(23)]
    public async Task GetSkinUrl()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/texture/skins");

        var content = await response.Content.ReadAsStringAsync();

        var data = JsonConvert.DeserializeObject<ResponseMessage<UrlServiceDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(data?.Data?.Url, Is.EqualTo(_skinUrl));
        });
    }

    [Test]
    [Order(24)]
    public async Task SetCloakUrl()
    {
        var httpContent = TestHelper.CreateJsonObject(new UrlServiceDto
        {
            Url = _cloakUrl = "https://example.com/cloak.png"
        });

        var response = await _httpClient.PutAsync("/api/v1/integrations/texture/cloaks", httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(25)]
    public async Task GetCloakUrl()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/texture/cloaks");

        var content = await response.Content.ReadAsStringAsync();

        var data = JsonConvert.DeserializeObject<ResponseMessage<UrlServiceDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(data?.Data?.Url, Is.EqualTo(_cloakUrl));
        });
    }

    [Test]
    [Order(26)]
    public async Task UpdateUserSkin()
    {
        var user = await Auth("GamerVII", "MegaPassword");

        using var fileStream = File.OpenRead("skin.png");
        using var fileContent = new StreamContent(fileStream);
        using var formData = new MultipartFormDataContent();

        formData.Add(fileContent, "Texture", Path.GetFileName(fileStream.Name));
        formData.Add(new StringContent("GamerVII"), "Login");

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", user.User!.Data!.AccessToken);

        // ToDo: Edit
        return;
        var response = await _httpClient.PostAsync("/api/v1/integrations/texture/skins/load", formData);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(27)]
    public async Task UpdateUserCloak()
    {
        // ToDo: Edit
        return;
        var httpContent = TestHelper.CreateJsonObject(new
        {
            CloakData = "Base64EncodedCloakData"
        });

        var response = await _httpClient.PostAsync("/api/v1/integrations/texture/cloak/load", httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(28)]
    public async Task GetMetaData()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/authlib/minecraft");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(29)]
    public async Task JoinMinecraftSession()
    {
        var user = await Auth("GamerVII", "MegaPassword");

        var httpContent = TestHelper.CreateJsonObject(new JoinRequest
        {
            AccessToken = user.User!.Data!.AccessToken,
            SelectedProfile = user.User.Data.Uuid,
            ServerId = _serverUuid
        });

        var response =
            await _httpClient.PostAsync("/api/v1/integrations/authlib/minecraft/sessionserver/session/minecraft/join",
                httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(30)]
    public async Task HasJoinedMinecraftSession()
    {
        var user = await Auth("GamerVII", "MegaPassword");

        var serverId = _serverUuid;
        var userName = user.User!.Data!.Name;

        var response =
            await _httpClient.GetAsync(
                $"/api/v1/integrations/authlib/minecraft/sessionserver/session/minecraft/hasJoined?username={userName}&serverId={serverId}");

        Assert.Multiple(() => { Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK)); });
    }

    [Test]
    [Order(31)]
    public async Task GetMinecraftProfile()
    {
        var user = await Auth("GamerVII", "MegaPassword");

        var response =
            await _httpClient.GetAsync(
                $"/api/v1/integrations/authlib/minecraft/sessionserver/session/minecraft/profile/{user.User!.Data!.Uuid}");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(32)]
    public async Task GetPlayersUuids()
    {
        var httpContent = TestHelper.CreateJsonObject(new
        {
            // Fill in with necessary properties
        });

        var response =
            await _httpClient.PostAsync("/api/v1/integrations/authlib/minecraft/profiles/minecraft", httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(33)]
    public async Task GetPlayerAttribute()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/authlib/minecraft/player/attributes");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(36)]
    public async Task GetIntegrationServices()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/auth");

        var content = await response.Content.ReadAsStringAsync();

        var data = JsonConvert.DeserializeObject<ResponseMessage<List<AuthServiceReadDto>>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.IsNotNull(data?.Data);
            Assert.IsNotEmpty(data!.Data!);
        });
    }

    [Test]
    [Order(37)]
    public async Task GetAuthService()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/auth/active");

        var content = await response.Content.ReadAsStringAsync();

        var data = JsonConvert.DeserializeObject<ResponseMessage<AuthServiceReadDto>>(content);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.IsNotNull(data?.Data);
        });
    }

    [Test]
    [Order(38)]
    public async Task RemoveAuthService()
    {
        var response = await _httpClient.DeleteAsync("/api/v1/integrations/auth/active");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(39)]
    public async Task GetProfiles()
    {
        var response = await _httpClient.GetAsync("/api/v1/profiles");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(40)]
    public async Task GetMinecraftVersions()
    {
        var response = await _httpClient.GetAsync($"/api/v1/profiles/versions/{GameLoader.Vanilla}/Vanilla");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(47)]
    public async Task DownloadFile()
    {
        var response = await _httpClient.GetAsync("/api/v1/file/{fileHash}");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(48)]
    public async Task AddToWhiteList()
    {
        var profile = TestHelper.CreateJsonObject(new ProfileCreateInfoDto
        {
            ProfileName = _profileName,
            GameAddress = "localhost",
            GamePort = 25565,
            RamSize = 4096,
            WindowWidth = 900,
            WindowHeight = 600,
            IsFullScreen = true,
            OsType = ((int)OsType.Windows).ToString(),
            OsArchitecture = Environment.Is64BitOperatingSystem ? "64" : "32",
            UserAccessToken = _accessToken,
            UserName = "GamerVII",
            UserUuid = "userUuid"
        });

        var profileResponse = await _httpClient.PostAsync("/api/v1/profiles/info", profile);
        var content = await profileResponse.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<ProfileReadInfoDto>>(content);

        // var httpContent = TestHelper.CreateJsonObject(new FileWhiteListDto
        // {
        //     ProfileName = model.Data.ProfileName,
        //     Hash = model.Data.Files.FirstOrDefault()?.Hash
        // });
        //
        // var response = await _httpClient.PostAsync("/api/v1/file/whiteList", httpContent);
        //
        // Assert.Multiple(() =>
        // {
        //     Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.InternalServerError));
        // });
    }

    [Test]
    [Order(49)]
    public async Task RemoveFromWhiteList()
    {
        var profile = TestHelper.CreateJsonObject(new ProfileCreateInfoDto
        {
            ProfileName = _profileName,
            GameAddress = "localhost",
            GamePort = 25565,
            RamSize = 4096,
            WindowWidth = 900,
            WindowHeight = 600,
            IsFullScreen = true,
            OsType = ((int)OsType.Windows).ToString(),
            OsArchitecture = Environment.Is64BitOperatingSystem ? "64" : "32",
            UserAccessToken = _accessToken,
            UserName = "GamerVII",
            UserUuid = "userUuid"
        });

        var profileResponse = await _httpClient.PostAsync("/api/v1/profiles/info", profile);
        var content = await profileResponse.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<ProfileReadInfoDto>>(content);

        // var httpContent = TestHelper.CreateJsonObject(new FileWhiteListDto
        // {
        //     ProfileName = model.Data.ProfileName,
        //     Hash = model.Data.Files.FirstOrDefault()?.Hash
        // });
        //
        // var response = await _httpClient.DeleteAsync("/api/v1/file/whiteList");
        //
        // Assert.Multiple(() =>
        // {
        //     Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.InternalServerError));
        // });
    }

    [Test]
    [Order(50)]
    public async Task GetPlatformSettings()
    {
        var response = await _httpClient.GetAsync("/api/v1/settings/platform");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(51)]
    public async Task UpdatePlatformSettings()
    {
        var httpContent = TestHelper.CreateJsonObject(new SettingsUpdateDto
        {
            RegistrationIsEnabled = true,
            StorageHost = "",
            StorageLogin = "",
            CurseForgeKey = "",
            StoragePassword = "",
            StorageType = (int)StorageType.LocalStorage
        });

        var response = await _httpClient.PutAsync("/api/v1/settings/platform", httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(52)]
    public async Task InstallPlugin()
    {
        return;
        var httpContent = TestHelper.CreateJsonObject(new
        {
            // Fill in with necessary properties
        });

        var response = await _httpClient.PostAsync("/api/v1/plugins/install", httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(53)]
    public async Task GetInstalledPlugins()
    {
        var response = await _httpClient.GetAsync("/api/v1/plugins");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

#if DEBUG
    [Test]
    [Order(54)]
    public async Task RemovePlugin()
    {
        var response = await _httpClient.DeleteAsync("/api/v1/plugins/{name}/{version}");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }
#endif

    [Test]
    [Order(55)]
    public async Task UploadLauncherVersion()
    {
        return;

        MultipartFormDataContent httpContent = default;

        var response = await _httpClient.PostAsync("/api/v1/launcher/upload/{osType}", httpContent);

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(56)]
    public async Task GetActualLauncherVersion()
    {
        var response = await _httpClient.GetAsync("/api/v1/launcher");

        Assert.Multiple(() => { Assert.That(response.IsSuccessStatusCode, Is.True); });
    }

    [Test]
    [Order(57)]
    public async Task CheckSentryException()
    {
        var response = await _httpClient.GetAsync("/api/v1/integrations/sentry/dsn");
        var content = await response.Content.ReadAsStringAsync();

        var model = JsonConvert.DeserializeObject<ResponseMessage<UrlServiceDto>>(content);

        // var address = $"{_httpClient.BaseAddress.Scheme}://gml@gmlb-test.recloud.tech/1";
        var address = $"{_httpClient.BaseAddress.Scheme}://gml@{_httpClient.BaseAddress.Host}/1";

        SentrySdk.Init(options =>
        {
            options.Dsn = address;
            options.Debug = true;
            options.TracesSampleRate = 1.0;
            options.DiagnosticLevel = SentryLevel.Debug;
            options.IsGlobalModeEnabled = true;
            options.SendDefaultPii = true;
            options.MaxAttachmentSize = 10 * 1024 * 1024;
        });

        await Task.Delay(TimeSpan.FromSeconds(2));
        SentrySdk.CaptureException(new Exception("TestMessage"));

        await Task.Delay(TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(model, Is.Not.Null);
            Assert.That(model?.Data, Is.Not.Null);
            Assert.That(model?.Data?.Url, Is.EqualTo(_newSentryUrl));
            Assert.That(response.IsSuccessStatusCode, Is.True);
        });
    }
}
