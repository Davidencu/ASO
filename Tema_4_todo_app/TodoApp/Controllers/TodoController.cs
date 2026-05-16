// Controllers/TodoController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Controllers;

[Authorize]
public class TodoController : Controller
{
    private readonly AppDbContext _db;

    public TodoController(AppDbContext db) => _db = db;

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /Todo  or  /Todo?filter=active|completed
    public async Task<IActionResult> Index(string? filter)
    {
        var query = _db.TodoItems
            .Where(t => t.UserId == CurrentUserId)
            .AsQueryable();

        query = filter switch
        {
            "active" => query.Where(t => !t.IsCompleted),
            "completed" => query.Where(t => t.IsCompleted),
            _ => query
        };

        ViewBag.Filter = filter ?? "all";
        return View(await query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt).ToListAsync());
    }

    // POST /Todo/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TodoItem item)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));
        item.UserId = CurrentUserId;
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST /Todo/Toggle/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var item = await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (item is null) return NotFound();
        item.IsCompleted = !item.IsCompleted;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST /Todo/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (item is not null)
        {
            _db.TodoItems.Remove(item);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // GET /Todo/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == CurrentUserId);
        if (item is null) return NotFound();
        return View(item);
    }

    // POST /Todo/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TodoItem item)
    {
        if (!ModelState.IsValid) return View(item);
        var existingItem = await _db.TodoItems
            .FirstOrDefaultAsync(t => t.Id == item.Id && t.UserId == CurrentUserId);
        if (existingItem is null) return NotFound();
        existingItem.Title = item.Title;
        existingItem.IsCompleted = item.IsCompleted;
        existingItem.DueDate = item.DueDate;
        existingItem.Priority = item.Priority;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
