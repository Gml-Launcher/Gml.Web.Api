using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using Gml.Web.Api.Domains.User;
using Gml.Web.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gml.Web.Api.Core.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DatabaseContext _databaseContext;
    private readonly IOptions<ServerSettings> _options;

    public UserRepository(DatabaseContext databaseContext, IOptions<ServerSettings> options)
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
            Subject = new ClaimsIdentity(new[] { new Claim("id", userId) }),
            Expires = DateTime.UtcNow.AddHours(72),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Value.SecretKey)),
                    SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}