using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Options;
using TodoApi.Services;

namespace TodoApi.Tests.Services;

[TestFixture]
public class TodoServiceTests
{
    private string _dbPath = null!;
    private TodoService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"todo_svc_test_{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={_dbPath}";
        DatabaseInitializer.EnsureCreated(connectionString);
        _sut = new TodoService(Microsoft.Extensions.Options.Options.Create(
            new TodoDatabaseOptions { ConnectionString = connectionString }));
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    [Test]
    public async Task CreateAsync_persists_and_sets_id_and_created_at()
    {
        var created = await _sut.CreateAsync(new CreateTodoRequest
        {
            Title = "a",
            Description = "b",
            IsCompleted = false
        });

        Assert.That(created.Id, Is.GreaterThan(0));
        Assert.That(created.Title, Is.EqualTo("a"));
        Assert.That(created.Description, Is.EqualTo("b"));
        Assert.That(created.IsCompleted, Is.False);
        Assert.That(created.CreatedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public async Task CreateAsync_allows_null_description()
    {
        var created = await _sut.CreateAsync(new CreateTodoRequest
        {
            Title = "no-desc",
            Description = null,
            IsCompleted = false
        });

        var roundTrip = await _sut.GetByIdAsync(created.Id);
        Assert.That(roundTrip, Is.Not.Null);
        Assert.That(roundTrip!.Description, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_returns_null_when_missing()
    {
        var result = await _sut.GetByIdAsync(int.MaxValue);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_returns_null_when_missing()
    {
        var result = await _sut.UpdateAsync(int.MaxValue, new UpdateTodoRequest
        {
            Title = "nope",
            Description = null,
            IsCompleted = true
        });
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_modifies_existing_row()
    {
        var created = await _sut.CreateAsync(new CreateTodoRequest
        {
            Title = "orig",
            Description = "d",
            IsCompleted = false
        });

        var updated = await _sut.UpdateAsync(created.Id, new UpdateTodoRequest
        {
            Title = "new",
            Description = null,
            IsCompleted = true
        });

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Id, Is.EqualTo(created.Id));
        Assert.That(updated.Title, Is.EqualTo("new"));
        Assert.That(updated.Description, Is.Null);
        Assert.That(updated.IsCompleted, Is.True);
    }

    [Test]
    public async Task DeleteAsync_returns_false_when_missing()
    {
        var deleted = await _sut.DeleteAsync(int.MaxValue);
        Assert.That(deleted, Is.False);
    }

    [Test]
    public async Task DeleteAsync_removes_row()
    {
        var created = await _sut.CreateAsync(new CreateTodoRequest
        {
            Title = "to-delete",
            Description = null,
            IsCompleted = false
        });

        Assert.That(await _sut.DeleteAsync(created.Id), Is.True);
        Assert.That(await _sut.GetByIdAsync(created.Id), Is.Null);
    }

    [Test]
    public async Task GetAllAsync_returns_all_rows_ordered_by_id()
    {
        await _sut.CreateAsync(new CreateTodoRequest { Title = "first", Description = null, IsCompleted = false });
        await _sut.CreateAsync(new CreateTodoRequest { Title = "second", Description = null, IsCompleted = false });

        var all = await _sut.GetAllAsync();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        var ids = all.Select(t => t.Id).ToList();
        Assert.That(ids, Is.EqualTo(ids.OrderBy(x => x).ToList()));
    }
}
