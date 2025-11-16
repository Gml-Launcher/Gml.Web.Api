using Gml.Domains.Exceptions;
using Gml.Domains.Repositories;
using Gml.Domains.User;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gml.Web.Api.Core.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DatabaseContext _databaseContext;
    private readonly ServerSettings _options;

    public UserRepository(DatabaseContext databaseContext, ServerSettings options)
    {
        _databaseContext = databaseContext;
        _options = options;
    }

    public Task<DbUser?> CheckExistUser(string login, string email)
    {
        return _databaseContext.Users.FirstOrDefaultAsync(c => c.Login == login || c.Email == email);
    }

    public async Task<DbUser?> GetUser(string loginOrEmail, string password)
    {
        var user = await _databaseContext.Users.FirstOrDefaultAsync(c =>
            c.Login == loginOrEmail || c.Email == loginOrEmail);

        if (user == null || !PasswordHasher.Verify(user.Password, password))
            return null;

        return user;
    }

    public async Task<DbUser?> GetUser(string loginOrEmail)
    {
        var user = await _databaseContext.Users.FirstOrDefaultAsync(c =>
            c.Login == loginOrEmail || c.Email == loginOrEmail);

        if (user == null)
        {
            return null;
        }

        return user;
    }

    public async Task<DbUser> CreateUser(string email, string login, string password)
    {
        var checkUser = await CheckExistUser(email, login);

        if (checkUser is not null)
            throw new UserAlreadyException();

        var entity = _databaseContext.Users.Add(new DbUser
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
