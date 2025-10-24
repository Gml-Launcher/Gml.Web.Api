using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gml.Web.Api.Domains.Auth;

namespace Gml.Web.Api.Domains.Repositories;

public interface IExternalApplicationRepository
{
    Task<ExternalApplication> CreateAsync(int userId, string name, string tokenHash, List<int> permissionIds);
    Task<List<ExternalApplication>> GetByUserIdAsync(int userId);
    Task<ExternalApplication?> GetByIdAsync(Guid id);
    Task<ExternalApplication?> GetByTokenHashAsync(string tokenHash);
    Task<bool> DeleteAsync(Guid id, int userId);
}
