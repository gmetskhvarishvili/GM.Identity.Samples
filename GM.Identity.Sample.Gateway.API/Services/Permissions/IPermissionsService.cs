using Refit;

namespace GM.Identity.Sample.Gateway.API.Services.Permissions;

public interface IPermissionsService
{
    [Post("/permissions")]
    Task<string> CreatePermission(CreatePermissionRequestModel request, CancellationToken cancellationToken);
    
    [Put("/permissions/{id}")]
    Task UpdatePermission(string id, UpdatePermissionRequestModel request, CancellationToken cancellationToken);
    
    [Delete("/permissions/{id}")]
    Task DeletePermission(string id, CancellationToken cancellationToken);
    
    [Get("/permissions")]
    Task<IEnumerable<PermissionResponseModel>> GetPermissionsList(GetPermissionsListRequestModel request, CancellationToken cancellationToken);
    
    [Get("/permissions/{id}")]
    Task<PermissionDetailsResponseModel> GetPermissionDetails(string id, CancellationToken cancellationToken);
}