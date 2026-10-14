using Mapster;
using Gml.Dto.News;
using Gml.Models.News;
using GmlCore.Interfaces.News;

namespace Gml.Web.Api.Core.MappingProfiles;

public class NewsMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<INewsData, NewsReadDto>();
        config.NewConfig<NewsData, NewsReadDto>();
    }
}
