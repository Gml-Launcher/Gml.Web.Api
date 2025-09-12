using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Exceptions;
using Gml.Web.Api.Domains.Repositories;
using Gml.Web.Api.Domains.User;
using Microsoft.EntityFrameworkCore;

namespace Gml.Web.Api.Core.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DatabaseContext _databaseContext;

    public UserRepository(DatabaseContext databaseContext, Gml.Web.Api.Core.Options.ServerSettings options)
    {
        _databaseContext = databaseContext;
    }

    public Task<User?> CheckExistUser(string login, string email)
    {
        return _databaseContext.Users.FirstOrDefaultAsync(c => c.Login == login || c.Email == email);
    }

    public async Task<User?> GetUser(string loginOrEmail, string password)
    {
        var user = await _databaseContext.Users.FirstOrDefaultAsync(c =>
            c.Login == loginOrEmail || c.Email == loginOrEmail);

        if (user == null || !PasswordHasher.Verify(user.Password, password))
            return null;

        return user;
    }

    public async Task<User?> GetUser(string loginOrEmail)
    {
        var user = await _databaseContext.Users.FirstOrDefaultAsync(c =>
            c.Login == loginOrEmail || c.Email == loginOrEmail);

        if (user == null)
        {
            return null;
        }

        return user;
    }

    public async Task<User> CreateUser(string email, string login, string password)
    {
        var checkUser = await CheckExistUser(email, login);

        if (checkUser is not null)
            throw new UserAlreadyException();

        var entity = _databaseContext.Users.Add(new User
        {
            Email = email,
            Login = login,
            Password = PasswordHasher.Hash(password),
            AccessToken = string.Empty
        });

        await _databaseContext.SaveChangesAsync();

        return entity.Entity;
    }
}
