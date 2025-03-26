using System.Net;
using AutoMapper;
using Gml.Core.Integrations;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.News;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;

namespace Gml.Web.Api.Core.Handlers;

public class NewsHandler : INewsHandler
{
    public static async Task<IResult> AddNewsListener(IGmlManager gmlManager, IMapper mapper, NewsListenerDto newsListenerDto)
    {
        switch (newsListenerDto.Type)
        {
            case NewsListenerType.Azuriom:
                await gmlManager.Integrations.NewsProvider.AddListener(new AzuriomNewsProvider(newsListenerDto.Url));
                break;
            case NewsListenerType.UnicoreCMS:
                await gmlManager.Integrations.NewsProvider.AddListener(new UnicoreNewsProvider(newsListenerDto.Url));
                break;
            case NewsListenerType.Custom:
                await gmlManager.Integrations.NewsProvider.AddListener(new CustomNewsProvider(newsListenerDto.Url));
                break;
            case NewsListenerType.VK:
                await gmlManager.Integrations.NewsProvider.AddListener(new VkNewsProvider(newsListenerDto.Url, gmlManager));
                break;
            case NewsListenerType.Telegram:
            default:
                return Results.BadRequest(ResponseMessage.Create("Не был найден данный provider новостей", HttpStatusCode.BadRequest));
        }

        return Results.Ok(ResponseMessage.Create("Provider был успешно добавлен", HttpStatusCode.OK));
    }

    public static async Task<IResult> RemoveNewsListener(IGmlManager gmlManager, IMapper mapper, NewsListenerType type)
    {
        await gmlManager.Integrations.NewsProvider.RemoveListenerByType(type);

        return Results.Ok(ResponseMessage.Create("Provider был успешно удален", HttpStatusCode.OK));
    }

    public static Task<IResult> GetNewsListener(IGmlManager gmlManager, IMapper mapper)
    {
        throw new NotImplementedException();
    }

    public static async Task<IResult> GetNews(IGmlManager gmlManager, IMapper mapper)
    {
        var news = await gmlManager.Integrations.NewsProvider.GetNews();

        return Results.Ok(ResponseMessage.Create(mapper.Map<List<NewsReadDto>>(news.ToList()), "Актуальные новости", HttpStatusCode.OK));
    }

    public static IResult GetListeners(IGmlManager gmlManager)
    {
        return Results.Ok(ResponseMessage.Create(gmlManager.Integrations.NewsProvider.Providers, "Актуальные новости", HttpStatusCode.OK));
    }
}
