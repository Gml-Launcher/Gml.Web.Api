using AutoMapper;
using Gml.Models.News;
using Gml.Web.Api.Dto.News;
using GmlCore.Interfaces.News;

namespace Gml.Web.Api.Core.MappingProfiles;

public class NewsMapper : Profile
{
    public NewsMapper()
    {
        CreateMap<INewsData, NewsReadDto>();
        CreateMap<NewsData, NewsReadDto>();
    }
}
