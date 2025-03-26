using AutoMapper;
using Gml.Web.Api.Dto.News;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Launcher;

namespace Gml.Web.Api.Core.Handlers;

public interface INewsHandler
{
    static abstract Task<IResult> AddNewsListener(IGmlManager gmlManager, IMapper mapper,
        NewsListenerDto newsListenerDto);
    static abstract Task<IResult> GetNewsListener(IGmlManager gmlManager, IMapper mapper);
    static abstract Task<IResult> GetNews(IGmlManager gmlManager, IMapper mapper);
}
