using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using TodoApi.Models;
using TodoApi.Options;

namespace TodoApi.Services;

public class TodoService : ITodoService
{
    private readonly string _connectionString;

    public TodoService(IOptions<TodoDatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString
            ?? throw new InvalidOperationException("TodoDatabase:ConnectionString is not configured.");
    }

    public async Task<Todo> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var createdAt = DateTime.UtcNow;
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Todos (Title, Description, IsCompleted, CreatedAt)
            VALUES ($title, $description, $isCompleted, $createdAt);
            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$title", request.Title);
        command.Parameters.AddWithValue("$description", (object?)request.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("$isCompleted", request.IsCompleted ? 1 : 0);
        command.Parameters.AddWithValue("$createdAt", createdAt.ToString("o", CultureInfo.InvariantCulture));

        var idObj = await command.ExecuteScalarAsync(cancellationToken);
        var id = Convert.ToInt32(idObj, CultureInfo.InvariantCulture);

        return new Todo
        {
            Id = id,
            Title = request.Title,
            Description = request.Description,
            IsCompleted = request.IsCompleted,
            CreatedAt = createdAt
        };
    }

    public async Task<IReadOnlyList<Todo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var todos = new List<Todo>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Description, IsCompleted, CreatedAt FROM Todos ORDER BY Id";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            todos.Add(MapTodo(reader));
        }

        return todos;
    }

    public async Task<Todo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Description, IsCompleted, CreatedAt FROM Todos WHERE Id = $id LIMIT 1";
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapTodo(reader);
        }

        return null;
    }

    public async Task<Todo?> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Todos
            SET Title = $title, Description = $description, IsCompleted = $isCompleted
            WHERE Id = $id
            """;
        command.Parameters.AddWithValue("$title", request.Title);
        command.Parameters.AddWithValue("$description", (object?)request.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("$isCompleted", request.IsCompleted ? 1 : 0);
        command.Parameters.AddWithValue("$id", id);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
        {
            return null;
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Todos WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static Todo MapTodo(SqliteDataReader reader)
    {
        return new Todo
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            IsCompleted = reader.GetInt32(3) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }
}
