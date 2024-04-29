using AutoMapper;
using FluentValidation;
using Gml.Web.Api.Dto.Texture;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public interface IErrorSaveHandler
{
    static abstract Task<IResult> GetDsnUrl(
        HttpContext context,
        IGmlManager gmlManager);

    static abstract Task<IResult> UpdateDsnUrl(
        HttpContext context,
        IGmlManager gmlManager,
        IMapper mapper,
        IValidator<UrlServiceDto> validator,
        UrlServiceDto urlDto);
}
