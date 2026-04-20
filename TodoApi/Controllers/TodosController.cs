using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly ITodoService _todoService;

    public TodosController(ITodoService todoService)
    {
        _todoService = todoService;
    }

    /// <summary>Create a new todo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Todo>> Create([FromBody] CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var created = await _todoService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>List all todos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Todo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Todo>>> GetAll(CancellationToken cancellationToken)
    {
        var todos = await _todoService.GetAllAsync(cancellationToken);
        return Ok(todos);
    }

    /// <summary>Get a todo by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Todo>> GetById(int id, CancellationToken cancellationToken)
    {
        var todo = await _todoService.GetByIdAsync(id, cancellationToken);
        if (todo is null)
        {
            return NotFound();
        }

        return Ok(todo);
    }

    /// <summary>Update a todo.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Todo>> Update(int id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        var updated = await _todoService.UpdateAsync(id, request, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    /// <summary>Delete a todo.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _todoService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
