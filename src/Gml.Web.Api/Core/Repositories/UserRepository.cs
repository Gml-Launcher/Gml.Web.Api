using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.User;
using Gml.Web.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gml.Web.Api.Core.Repositories;

public class UserRepository(DatabaseContext databaseContext, IOptions<ServerSettings> options) : IUserRepository
{
    public Task<User?> CheckExistUser(string login, string email)
    {
        return databaseContext.Users.FirstOrDefaultAsync(c => c.Login == login || c.Email == email);
    }

    public async Task<User?> GetUser(string loginOrEmail, string password)
    {
        var user = await databaseContext.Users.FirstOrDefaultAsync(c =>
            c.Login == loginOrEmail || c.Email == loginOrEmail);

        if (user == null || !PasswordHasher.Verify(user.Password, password)) 
            return null;
        
        user.AccessToken = AccessTokenService.Generate(user.Login, options.Value.SecretGmlKey);
        await databaseContext.SaveChangesAsync();
        
        return user;

    }

    public async Task<User> CreateUser(string email, string login, string password)
    {
        var checkUser = await CheckExistUser(email, login);

        if (checkUser is not null)
            throw new UserAlreadyException();

        var entity = databaseContext.Users.Add(new User
        {
            Email = email,
            Login = login,
            Password = PasswordHasher.Hash(password),
            AccessToken = AccessTokenService.Generate(login, options.Value.SecretGmlKey)
        });

        await databaseContext.SaveChangesAsync();

        return entity.Entity;
    }
}