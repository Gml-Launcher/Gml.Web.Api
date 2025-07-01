using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.Exceptions;
using Gml.Web.Api.Domains.Repositories;
using Gml.Web.Api.Domains.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gml.Web.Api.Core.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationContext _context;
    private readonly DatabaseContext _databaseContext;
    private readonly ServerSettings _options;

    public UserRepository(DatabaseContext databaseContext, ServerSettings options)
    {
        _databaseContext = databaseContext;
        _options = options;
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

        user.AccessToken = GetAccessToken(user.Id.ToString());

        await _databaseContext.SaveChangesAsync();

        return user;
    }

    public async Task<User?> GetUser(string loginOrEmail)
    {
        var user = await _databaseContext.Users.FirstOrDefaultAsync(c =>
            c.Login == loginOrEmail || c.Email == loginOrEmail);

        if (user == null)
        {
            if (user == null)
                return null;
        }

        user.AccessToken = GetAccessToken(user.Id.ToString());

        await _databaseContext.SaveChangesAsync();

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
            Password = PasswordHasher.Hash(password)
        });

        entity.Entity.AccessToken = GetAccessToken(entity.Entity.Id.ToString());

        await _databaseContext.SaveChangesAsync();

        return entity.Entity;
    }

    private string GetAccessToken(string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", userId),
                new Claim(ClaimTypes.Role, "Admin"),
            }),
            Expires = DateTime.UtcNow.AddDays(3),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecurityKey)),
                    SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
