using System.Threading.Tasks;

namespace Gml.Web.Api.Domains.Repositories;

public interface IUserRepository
{
    Task<User.User?> CheckExistUser(string login, string email);
    Task<User.User?> GetUser(string loginOrEmail, string password);
    Task<User.User> CreateUser(string email, string login, string password);
    Task<User.User?> GetUser(string loginOrEmail);
}
