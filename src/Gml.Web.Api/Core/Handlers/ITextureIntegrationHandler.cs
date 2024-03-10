using FluentValidation;
using Gml.Web.Api.Domains.Texture;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public interface ITextureIntegrationHandler
{
        static abstract Task<IResult> GetSkinUrl(IGmlManager gmlManager);
        static abstract Task<IResult> SetSkinUrl(
            IGmlManager gmlManager,
            IValidator<TextureServiceDto> validator,
            TextureServiceDto textureDto
            );

        static abstract Task<IResult> GetCloakUrl(IGmlManager gmlManager);
        static abstract Task<IResult> SetCloakUrl(
            IGmlManager gmlManager,
            IValidator<TextureServiceDto> validator,
            TextureServiceDto textureDto);
}
