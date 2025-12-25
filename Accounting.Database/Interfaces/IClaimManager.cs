using Accounting.Business;

namespace Accounting.Database.Interfaces
{
  public interface IClaimManager : IGenericRepository<Claim, int>
  {
    Task<int> CreateClaimAsync(int userId, string claimType, string claimValue, int organizationId, int createdById);
    Task<int> CreateClaimAsync(int userId, string claimType, string claimValue, int organizationId, int createdById, string databaseName);
    Task<int> DeleteClaimAsync(int userID, string key, string value, int organizationId);
    Task<int> DeleteRoles(int userID, int organizationId);
    Task<List<Claim>> GetAllAsync(int userID, int organizationId, string claimType);
    Task<Claim> GetAsync(int userId, string databaseName, string inRole);
    Task<Claim> GetAsync(int userID, int organizationId, string claimType, string claimValue);
    Task<int> GetUserCountWithRoleAsync(string role, int currentOrganizationId);
  }
}