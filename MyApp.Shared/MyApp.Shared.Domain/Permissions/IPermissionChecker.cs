

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid userId, string module, string action);
    Task<bool> HasPermissionAsync(string? username, string module, string action);
}