using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public interface IUserStore
    {
        IEnumerable<User> Query(out int totalCount, int pageNumber, int pageSize, bool? isActive, string? q);
        bool TryGet(int id, out User? user);
        bool TryCreate(CreateUserRequest req, out User created, out string? error);
        bool TryUpdate(int id, UpdateUserRequest req, out string? error);
        bool TryDelete(int id);
    }
}
