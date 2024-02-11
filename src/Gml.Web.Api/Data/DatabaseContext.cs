using Gml.Web.Api.Domains.User;
using Microsoft.EntityFrameworkCore;

namespace Gml.Web.Api.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}