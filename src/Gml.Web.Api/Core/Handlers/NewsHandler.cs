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
                await gmlManager.Integrations.NewsProvider.AddListener(new AzuriomNewsProvider(newsListenerDto.Url, newsListenerDto.Type));
                break;
            case NewsListenerType.UnicoreCMS:
                await gmlManager.Integrations.NewsProvider.AddListener(new UnicoreNewsProvider(newsListenerDto.Url, newsListenerDto.Type));
                break;
            case NewsListenerType.Custom:
                await gmlManager.Integrations.NewsProvider.AddListener(new CustomNewsProvider(newsListenerDto.Url, newsListenerDto.Type));
                break;
            default:
                return Results.BadRequest(ResponseMessage.Create("Не был найден данный provider новостей", HttpStatusCode.BadRequest));
        }

        return Results.Ok(ResponseMessage.Create("Provider был успешно добавлен", HttpStatusCode.OK));
    }

    public static Task<IResult> GetNewsListener(IGmlManager gmlManager, IMapper mapper)
    {
        throw new NotImplementedException();
    }

    public static async Task<IResult> GetNews(IGmlManager gmlManager)
    {
        var news = await gmlManager.Integrations.NewsProvider.GetNews();

        return Results.Ok(ResponseMessage.Create(news, "Актуальные новости", HttpStatusCode.OK));
    }
}
