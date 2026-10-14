using Mapster;
using Gml.Dto.Integration;
using Gml.Models.Auth;

namespace Gml.Web.Api.Core.MappingProfiles;

public class AuthServerMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AuthServiceInfo, AuthServiceReadDto>();
    }
}
