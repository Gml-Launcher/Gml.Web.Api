using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Repositories;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Player;
using Gml.Web.Api.Dto.User;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public class AuthHandler : IAuthHandler
{
    public static async Task<IResult> CreateUser(
        IUserRepository userRepository,
        IValidator<UserCreateDto> validator,
        IMapper mapper,
        UserCreateDto createDto,
        ApplicationContext appContext)
    {
        if (appContext.Settings.RegistrationIsEnabled == false)
            return Results.BadRequest(ResponseMessage.Create("Регистрация для новых пользователей запрещена",
                HttpStatusCode.BadRequest));

        var result = await validator.ValidateAsync(createDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var user = await userRepository.CheckExistUser(createDto.Login, createDto.Email);

        if (user is not null)
            return Results.BadRequest(ResponseMessage.Create("Пользователь с указанными данными уже существует",
                HttpStatusCode.BadRequest));

        user = await userRepository.CreateUser(createDto.Email, createDto.Login, createDto.Password);

        return Results.Ok(ResponseMessage.Create(mapper.Map<UserAuthReadDto>(user), "Успешная регистрация",
            HttpStatusCode.OK));
    }

    public static async Task<IResult> UserInfo(IGmlManager manager, IMapper mapper, string userName)
    {
        var user = await manager.Users.GetUserByName(userName);

        if (user is null)
        {
            return Results.NotFound(ResponseMessage.Create("Пользователь не найден", HttpStatusCode.BadRequest));
        }

        return Results.Ok(ResponseMessage.Create(mapper.Map<PlayerTextureDto>(user), "Успешная авторизация",
            HttpStatusCode.OK));
    }

    public static async Task<IResult> AuthUser(
        IUserRepository userRepository,
        IValidator<UserAuthDto> validator,
        IMapper mapper,
        UserAuthDto authDto)
    {
        var result = await validator.ValidateAsync(authDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var user = await userRepository.GetUser(authDto.Login, authDto.Password);

        if (user is null)
            return Results.BadRequest(ResponseMessage.Create("Неверный логин или пароль",
                HttpStatusCode.BadRequest));

        return Results.Ok(ResponseMessage.Create(mapper.Map<UserAuthReadDto>(user), "Успешная авторизация",
            HttpStatusCode.OK));
    }

    public static Task<IResult> UpdateUser(IUserRepository userRepository, UserUpdateDto userUpdateDto)
    {
        throw new NotImplementedException();
    }
}
