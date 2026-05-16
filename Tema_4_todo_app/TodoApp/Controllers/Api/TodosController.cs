// Controllers/Api/TodosController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/todos")]
public class TodosController : ControllerBase
{
    private readonly AppDbContext _db;

    public TodosController(AppDbContext db) => _db = db;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/todos?filter=active|completed
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetAll(string? filter)
    {
        var query = _db.TodoItems.Where(t => t.UserId == CurrentUserId);

        query = filter switch
        {
            "active" => query.Where(t => !t.IsCompleted),
            "completed" => query.Where(t => t.IsCompleted),
            _ => query
        };

        return await query
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    // GET /api/todos/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItem>> GetById(int id)
    {
        var item = await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        return item is null ? NotFound() : item;
    }

    // POST /api/todos
    [HttpPost]
    public async Task<ActionResult<TodoItem>> Create(TodoRequest req)
    {
        var item = new TodoItem
        {
            Title = req.Title,
            IsCompleted = req.IsCompleted,
            DueDate = req.DueDate,
            Priority = req.Priority,
            UserId = CurrentUserId
        };
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    // PUT /api/todos/5
    [HttpPut("{id}")]
    public async Task<ActionResult<TodoItem>> Update(int id, TodoRequest req)
    {
        var item = await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (item is null) return NotFound();
        item.Title = req.Title;
        item.IsCompleted = req.IsCompleted;
        item.DueDate = req.DueDate;
        item.Priority = req.Priority;
        await _db.SaveChangesAsync();
        return item;
    }

    // PATCH /api/todos/5/toggle
    [HttpPatch("{id}/toggle")]
    public async Task<ActionResult<TodoItem>> Toggle(int id)
    {
        var item = await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (item is null) return NotFound();
        item.IsCompleted = !item.IsCompleted;
        await _db.SaveChangesAsync();
        return item;
    }

    // DELETE /api/todos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (item is null) return NotFound();
        _db.TodoItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record TodoRequest(
    [Required][StringLength(200, MinimumLength = 1)] string Title,
    bool IsCompleted,
    DateTime? DueDate,
    Priority Priority
);
