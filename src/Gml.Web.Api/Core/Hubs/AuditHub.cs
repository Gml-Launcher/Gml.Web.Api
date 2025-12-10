using System.Net;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class AuditHub(IGmlManager gmlManager, IServiceProvider provider) : BaseHub
{
    private IReadOnlyCollection<AuditStageBase> _stages =
    [
        new SiteAvailableAuditStage(gmlManager),
        new GutHubAvailableAuditStage(),
        new TexturesAuditStage(gmlManager),
        new DiscordAuditStage(gmlManager),
        new NewsAuditStage(gmlManager),
        new SentryAuditStage(gmlManager),
        new ProfileAuditStage(gmlManager),
        new MinecraftServerStage(gmlManager),
        new FileSizeAuditStage(gmlManager),
        new LauncherUrlsAuditStage(gmlManager),
        new ProtocolAuditStage(gmlManager, provider),
        new ExternalServiceAuditStage(gmlManager, provider)
    ];

    public async Task Start(string address)
    {
        try
        {
            ChangeProgress(nameof(AuditHub), 0);

            var totalStages = _stages.Count;
            var currentStage = 0;
            var lockObject = new object();
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 5
            };

            await Parallel.ForEachAsync(_stages, parallelOptions, async (stage, cancellationToken) =>
            {
                stage.Host = address;
                await stage.Evaluate();

                int localCurrentStage;
                lock (lockObject)
                {
                    currentStage++;
                    localCurrentStage = currentStage;
                }

                var progress = (int)((double)localCurrentStage / totalStages * 100);
                ChangeProgress(nameof(AuditHub), progress);
                SendResult(stage);
            });

            SendCallerMessage("Аудит завершен");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    protected async void SendResult(AuditStageBase stage)
    {
        try
        {
            await Clients.All.SendAsync($"AuditResult", stage.Name, stage.Description, stage.SubStages,
                JsonConvert.SerializeObject(stage.Result));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}

public class AuditResult()
{
    public List<AuditMessage> Messages = [];
}

public enum AuditMessageType
{
    Default = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}

public class AuditMessage
{
    public AuditMessage(AuditMessageType type, string message)
    {
        Type = type;
        Message = message;
    }

    public AuditMessageType Type { get; }
    public string Message { get; }
}

public static class EndpointHealthChecker
{
    public static async Task<EndpointCheckResult> CheckEndpointAsync(string endpoint,
        HttpStatusCode[] allowedStatusCodes)
    {
        var result = new EndpointCheckResult { Host = endpoint };

        Uri uri;
        try
        {
            uri = new Uri(endpoint);
        }
        catch (UriFormatException ex)
        {
            result.Success = false;
            result.Message = $"Некорректный формат URI: {endpoint}. Ошибка: {ex.Message}";
            return result;
        }

        if (!uri.IsAbsoluteUri)
        {
            result.Success = false;
            result.Message = $"URI должен быть абсолютным: {endpoint}";
            return result;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            result.Success = false;
            result.Message = $"URI должен использовать HTTP или HTTPS протокол: {endpoint}";
            return result;
        }

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "GML-Audit");

        try
        {
            var response = await client.GetAsync(uri);
            result.StatusCode = (int)response.StatusCode;
            result.Success = response.IsSuccessStatusCode || allowedStatusCodes.Contains(response.StatusCode);

            if (response.IsSuccessStatusCode)
                result.Message = $"HTTP {(int)response.StatusCode} от {uri.Host}";
            else
            {
                result.Message = $"Получен HTTP {(int)response.StatusCode} от {uri.Host}";
            }
        }
        catch (HttpRequestException ex)
        {
            result.Success = false;
            result.Message = $"Ошибка HTTP запроса: {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            result.Success = false;
            result.Message = $"Превышено время ожидания ответа от {uri.Host}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Непредвиденная ошибка при проверке доступности: {ex.Message}";
        }

        return result;
    }

    public class EndpointCheckResult
    {
        public bool Success { get; set; }
        public string Host { get; set; } = string.Empty;
        public int? StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
