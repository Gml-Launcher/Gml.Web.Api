using AutoMapper;
using FluentValidation;
using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Repositories;
using Gml.Web.Api.Dto.User;

namespace Gml.Web.Api.Core.Handlers;

public interface IAuthHandler
{
    static abstract Task<IResult> CreateUser(
        IUserRepository userRepository,
        IValidator<UserCreateDto> validator,
        IMapper mapper,
        UserCreateDto createDto,
        ApplicationContext appContext);

    static abstract Task<IResult> AuthUser(
        IUserRepository userRepository,
        IValidator<UserAuthDto> validator,
        IMapper mapper,
        UserAuthDto authDto);

    static abstract Task<IResult> UpdateUser(
        IUserRepository userRepository,
        UserUpdateDto userUpdateDto);
}
