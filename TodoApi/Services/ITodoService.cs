using TodoApi.Models;

namespace TodoApi.Services;

public interface ITodoService
{
    Task<Todo> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Todo>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Todo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Todo?> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
