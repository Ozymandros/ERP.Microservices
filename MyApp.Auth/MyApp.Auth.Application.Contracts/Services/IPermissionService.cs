
namespace MyApp.Auth.Application.Contracts
{
    public interface IPermissionService
    {
        public async Task<bool> HasPermissionAsync(Guid userId, string module, string action) => throw new NotImplementedException();
    }
}