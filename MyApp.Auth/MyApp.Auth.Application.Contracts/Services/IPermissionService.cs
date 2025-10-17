
namespace MyApp.Auth.Application.Contracts
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(Guid userId, string module, string action);

        Task<bool> HasPermissionAsync(string? username, string module, string action);
    }
}