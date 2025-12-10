using System.Net;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Hubs.Audit;

public class TexturesAuditStage : AuditStageBase
{
    private readonly IGmlManager _gmlManager;

    public TexturesAuditStage(IGmlManager gmlManager) : base(
        "Проверка игровых текстур", "Проверка что скины и плащи подгружаются с вашего сайта",
        [
            "Проверка доступности ендпоинта скина",
            "Проверка доступности ендпоинта плаща",
            "Проверка на возврат default скина, в случае отсутствия пользователя",
            "Проверка на получение изображения"
        ]
    )
    {
        _gmlManager = gmlManager;
    }

    public override async Task Evaluate()
    {
        var users = await _gmlManager.Users.GetUsers(1, 0, string.Empty);
        var user = users.FirstOrDefault();
        try
        {
            var skinService = await _gmlManager.Integrations.GetSkinServiceAsync();
            var address = skinService
                .Replace("{userName}", "GamerVII")
                .Replace("{userUuid}", Guid.NewGuid().ToString());

            var res = await EndpointHealthChecker.CheckEndpointAsync(
                address, []
            );

            if (res.Success)
                AddSuccess($"{res.Message}: {address}");
            else
                AddError($"{res.Message}: {address}");

            if (user is null)
            {
                AddError($"В системе отсутствуют игроки, проверка скина пропущена");
            }
            else
            {
                try
                {
                    await _gmlManager.Profiles.CreateUserSessionAsync(null, user, Host);

                    var stream =
                        await _gmlManager.Integrations.TextureProvider.GetSkinStream(user.ExternalTextureSkinUrl);

                    var buffer = new byte[1];
                    var bytesRead = await stream.ReadAsync(buffer, 0, 1);

                    if (bytesRead == 0)
                    {
                        AddError($"Не удалось получить скин для пользователя {user.Name}");
                    }
                }
                catch (Exception e)
                {
                    AddError("Не удалось получить скин с вашего сайта");
                    Console.WriteLine(e);
                }
            }
        }
        catch (Exception e)
        {
            AddError($"Адрес для скинов не настроен: {e.Message}");
        }

        try
        {
            var cloakService = await _gmlManager.Integrations.GetCloakServiceAsync();
            var address = cloakService
                .Replace("{userName}", "GamerVII")
                .Replace("{userUuid}", Guid.NewGuid().ToString());
            var res = await EndpointHealthChecker.CheckEndpointAsync(
                address, [HttpStatusCode.NotFound]
            );
            if (res.Success)
                AddSuccess($"{res.Message}: {address}");
            else
                AddError($"{res.Message}: {address}");

            if (user is null)
            {
                AddError($"В системе отсутствуют игроки, проверка скина пропущена");
            }
            else
            {
                try
                {
                    await _gmlManager.Profiles.CreateUserSessionAsync(null, user, Host);

                    var stream =
                        await _gmlManager.Integrations.TextureProvider.GetSkinStream(user.ExternalTextureSkinUrl);

                    var buffer = new byte[1];
                    var bytesRead = await stream.ReadAsync(buffer, 0, 1);

                    if (bytesRead == 0)
                    {
                        AddError($"Не удалось получить плащ для пользователя {user.Name}");
                    }
                }
                catch (Exception e)
                {
                    AddError("Не удалось получить плащ с вашего сайта");
                    Console.WriteLine(e);
                }
            }
        }
        catch (Exception e)
        {
            AddError($"Адрес для плащей не настроен: {e.Message}");
        }
    }
}
