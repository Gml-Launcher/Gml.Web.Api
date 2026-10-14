using Mapster;
using Gml.Domains.User;
using Gml.Dto.User;

namespace Gml.Web.Api.Core.MappingProfiles;

public class UserMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DbUser, UserAuthReadDto>();
    }
}
