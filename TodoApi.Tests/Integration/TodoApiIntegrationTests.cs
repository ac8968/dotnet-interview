using System.Net;
using System.Net.Http.Json;
using TodoApi.Models;

namespace TodoApi.Tests.Integration;

[TestFixture]
[NonParallelizable]
public class TodoApiIntegrationTests
{
    private TodoApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new TodoApiWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Create_returns_201_and_round_trips()
    {
        var request = new CreateTodoRequest { Title = "integration", Description = "d", IsCompleted = false };
        var response = await _client.PostAsJsonAsync("/api/todos", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await response.Content.ReadFromJsonAsync<Todo>();
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Id, Is.GreaterThan(0));
        Assert.That(created.Title, Is.EqualTo("integration"));

        var get = await _client.GetFromJsonAsync<Todo>($"/api/todos/{created.Id}");
        Assert.That(get, Is.Not.Null);
        Assert.That(get!.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task Get_all_returns_ok()
    {
        var response = await _client.GetAsync("/api/todos");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Get_by_id_returns_404_when_missing()
    {
        var response = await _client.GetAsync("/api/todos/999999");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Update_returns_404_when_missing()
    {
        var response = await _client.PutAsJsonAsync("/api/todos/999999", new UpdateTodoRequest
        {
            Title = "x",
            Description = "y",
            IsCompleted = true
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Update_validation_fails_for_empty_title()
    {
        var create = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest
        {
            Title = "valid-before-update",
            Description = null,
            IsCompleted = false
        });
        create.EnsureSuccessStatusCode();
        var todo = await create.Content.ReadFromJsonAsync<Todo>();
        Assert.That(todo, Is.Not.Null);

        var response = await _client.PutAsJsonAsync($"/api/todos/{todo!.Id}", new UpdateTodoRequest
        {
            Title = "",
            Description = null,
            IsCompleted = false
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Delete_returns_404_when_missing()
    {
        var response = await _client.DeleteAsync("/api/todos/999999");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Create_validation_fails_for_empty_title()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest
        {
            Title = "",
            Description = null
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Full_crud_flow()
    {
        var create = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest
        {
            Title = "flow",
            Description = null,
            IsCompleted = false
        });
        create.EnsureSuccessStatusCode();
        var todo = await create.Content.ReadFromJsonAsync<Todo>();
        Assert.That(todo, Is.Not.Null);

        var update = await _client.PutAsJsonAsync($"/api/todos/{todo!.Id}", new UpdateTodoRequest
        {
            Title = "flow-updated",
            Description = "done",
            IsCompleted = true
        });
        update.EnsureSuccessStatusCode();
        var updated = await update.Content.ReadFromJsonAsync<Todo>();
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.IsCompleted, Is.True);

        var delete = await _client.DeleteAsync($"/api/todos/{todo.Id}");
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var get = await _client.GetAsync($"/api/todos/{todo.Id}");
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
