using Microsoft.AspNetCore.Mvc;
using TodoApi.Controllers;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Tests.Controllers;

[TestFixture]
public class TodosControllerTests
{
    private Mock<ITodoService> _todoService = null!;
    private TodosController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _todoService = new Mock<ITodoService>();
        _controller = new TodosController(_todoService.Object);

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("http://localhost/api/todos/42");
        _controller.Url = urlHelper.Object;
    }

    [Test]
    public async Task Create_calls_service_and_returns_201_with_body()
    {
        var request = new CreateTodoRequest { Title = "t", Description = "d", IsCompleted = false };
        var created = new Todo
        {
            Id = 42,
            Title = "t",
            Description = "d",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _todoService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await _controller.Create(request, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdAt = (CreatedAtActionResult)result.Result!;
        Assert.That(createdAt.StatusCode, Is.EqualTo(201));
        Assert.That(createdAt.Value, Is.SameAs(created));
        _todoService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAll_returns_ok_with_list_from_service()
    {
        IReadOnlyList<Todo> list =
        [
            new Todo { Id = 1, Title = "a", Description = null, IsCompleted = false, CreatedAt = DateTime.UtcNow }
        ];
        _todoService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var result = await _controller.GetAll(CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(list));
        _todoService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetById_returns_not_found_when_service_returns_null()
    {
        _todoService.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Todo?)null);

        var result = await _controller.GetById(99, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        _todoService.Verify(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetById_returns_ok_when_found()
    {
        var todo = new Todo { Id = 5, Title = "x", Description = null, IsCompleted = true, CreatedAt = DateTime.UtcNow };
        _todoService.Setup(s => s.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(todo);

        var result = await _controller.GetById(5, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(todo));
    }

    [Test]
    public async Task Update_returns_not_found_when_service_returns_null()
    {
        var request = new UpdateTodoRequest { Title = "u", Description = null, IsCompleted = false };
        _todoService
            .Setup(s => s.UpdateAsync(7, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Todo?)null);

        var result = await _controller.Update(7, request, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        _todoService.Verify(s => s.UpdateAsync(7, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Update_returns_ok_when_service_returns_todo()
    {
        var request = new UpdateTodoRequest { Title = "u", Description = "d", IsCompleted = true };
        var updated = new Todo { Id = 3, Title = "u", Description = "d", IsCompleted = true, CreatedAt = DateTime.UtcNow };
        _todoService
            .Setup(s => s.UpdateAsync(3, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await _controller.Update(3, request, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.SameAs(updated));
    }

    [Test]
    public async Task Delete_returns_not_found_when_service_returns_false()
    {
        _todoService.Setup(s => s.DeleteAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.Delete(8, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        _todoService.Verify(s => s.DeleteAsync(8, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Delete_returns_no_content_when_service_returns_true()
    {
        _todoService.Setup(s => s.DeleteAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.Delete(8, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _todoService.Verify(s => s.DeleteAsync(8, It.IsAny<CancellationToken>()), Times.Once);
    }
}
