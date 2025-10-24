using AutoMapper;
using Gml.Dto.News;
using Gml.Models.News;
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
