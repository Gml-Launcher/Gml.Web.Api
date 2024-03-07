using AutoMapper;
using FluentValidation;
using Gml.Web.Api.Core.Integrations.Auth;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.User;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public interface IAuthIntegrationHandler
{
    static abstract Task<IResult> Auth(
        HttpContext context,
        IGmlManager gmlManager,
        IMapper mapper,
        IValidator<BaseUserPassword> validator,
        IAuthService authService,
        BaseUserPassword authDto);

    static abstract Task<IResult> GetIntegrationServices(IGmlManager gmlManager, IMapper mapper);

    static abstract Task<IResult> GetAuthService(IGmlManager gmlManager, IMapper mapper);

    static abstract Task<IResult> SetAuthService(
        IGmlManager gmlManager,
        IValidator<IntegrationUpdateDto> validator,
        IntegrationUpdateDto updateDto);

    static abstract Task<IResult> RemoveAuthService(IGmlManager gmlManager);
}
