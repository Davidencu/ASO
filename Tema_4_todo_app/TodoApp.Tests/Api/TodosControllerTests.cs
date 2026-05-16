using Microsoft.AspNetCore.Mvc;
using TodoApp.Controllers.Api;
using TodoApp.Data;
using TodoApp.Models;
using TodoApp.Tests.Helpers;

namespace TodoApp.Tests.Api;

public class TodosControllerTests
{
    private static (TodosController ctrl, AppDbContext db) Setup(string userId = "user-1")
    {
        var db = TestHelpers.CreateDb();
        return (new TodosController(db).WithUser(userId), db);
    }

    // ── GetAll ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentUserItems()
    {
        var (ctrl, db) = Setup("u1");
        db.TodoItems.AddRange(
            new TodoItem { Title = "Mine",  UserId = "u1" },
            new TodoItem { Title = "Other", UserId = "u2" });
        await db.SaveChangesAsync();

        var result = await ctrl.GetAll(null);

        var items = Assert.IsAssignableFrom<IEnumerable<TodoItem>>(result.Value).ToList();
        Assert.Single(items);
        Assert.Equal("Mine", items[0].Title);
    }

    [Fact]
    public async Task GetAll_FilterActive_ExcludesCompleted()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.AddRange(
            new TodoItem { Title = "Active",    UserId = "user-1", IsCompleted = false },
            new TodoItem { Title = "Completed", UserId = "user-1", IsCompleted = true  });
        await db.SaveChangesAsync();

        var result = await ctrl.GetAll("active");

        var items = Assert.IsAssignableFrom<IEnumerable<TodoItem>>(result.Value).ToList();
        Assert.Single(items);
        Assert.False(items[0].IsCompleted);
    }

    [Fact]
    public async Task GetAll_FilterCompleted_ExcludesActive()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.AddRange(
            new TodoItem { Title = "Active",    UserId = "user-1", IsCompleted = false },
            new TodoItem { Title = "Completed", UserId = "user-1", IsCompleted = true  });
        await db.SaveChangesAsync();

        var result = await ctrl.GetAll("completed");

        var items = Assert.IsAssignableFrom<IEnumerable<TodoItem>>(result.Value).ToList();
        Assert.Single(items);
        Assert.True(items[0].IsCompleted);
    }

    // ── GetById ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingItem_ReturnsIt()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "Task", UserId = "user-1" });
        await db.SaveChangesAsync();

        var result = await ctrl.GetById(1);

        Assert.NotNull(result.Value);
        Assert.Equal("Task", result.Value!.Title);
    }

    [Fact]
    public async Task GetById_MissingId_ReturnsNotFound()
    {
        var (ctrl, _) = Setup();

        var result = await ctrl.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_OtherUsersItem_ReturnsNotFound()
    {
        var (ctrl, db) = Setup("u1");
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "u2" });
        await db.SaveChangesAsync();

        var result = await ctrl.GetById(1);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithItem()
    {
        var (ctrl, db) = Setup();

        var result = await ctrl.Create(new TodoRequest("Buy milk", false, null, Priority.Medium));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var item = Assert.IsType<TodoItem>(created.Value);
        Assert.Equal("Buy milk", item.Title);
        Assert.Equal("user-1", item.UserId);
        Assert.Single(db.TodoItems);
    }

    [Fact]
    public async Task Create_SetsUserIdFromClaims_NotFromRequest()
    {
        var (ctrl, db) = Setup("owner-99");

        await ctrl.Create(new TodoRequest("Task", false, null, Priority.Low));

        Assert.Equal("owner-99", db.TodoItems.Single().UserId);
    }

    // ── Update ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingItem_ReturnsUpdatedItem()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "Old", UserId = "user-1", IsCompleted = false });
        await db.SaveChangesAsync();

        var result = await ctrl.Update(1, new TodoRequest("New", true, null, Priority.High));

        Assert.Equal("New", result.Value!.Title);
        Assert.True(result.Value.IsCompleted);
        Assert.Equal(Priority.High, result.Value.Priority);
        var saved = db.TodoItems.Single();
        Assert.Equal("New", saved.Title);
    }

    [Fact]
    public async Task Update_MissingId_ReturnsNotFound()
    {
        var (ctrl, _) = Setup();

        var result = await ctrl.Update(999, new TodoRequest("X", false, null, Priority.Low));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_OtherUsersItem_ReturnsNotFound()
    {
        var (ctrl, db) = Setup("u1");
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "u2" });
        await db.SaveChangesAsync();

        var result = await ctrl.Update(1, new TodoRequest("Hacked", false, null, Priority.Low));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Toggle ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Toggle_FlipsIsCompleted()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "user-1", IsCompleted = false });
        await db.SaveChangesAsync();

        var result = await ctrl.Toggle(1);

        Assert.True(result.Value!.IsCompleted);
        Assert.True(db.TodoItems.Single().IsCompleted);
    }

    [Fact]
    public async Task Toggle_CalledTwice_RestoresOriginalState()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "user-1", IsCompleted = false });
        await db.SaveChangesAsync();

        await ctrl.Toggle(1);
        await ctrl.Toggle(1);

        Assert.False(db.TodoItems.Single().IsCompleted);
    }

    [Fact]
    public async Task Toggle_MissingId_ReturnsNotFound()
    {
        var (ctrl, _) = Setup();

        var result = await ctrl.Toggle(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingItem_Returns204AndRemoves()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "user-1" });
        await db.SaveChangesAsync();

        var result = await ctrl.Delete(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.TodoItems);
    }

    [Fact]
    public async Task Delete_MissingId_ReturnsNotFound()
    {
        var (ctrl, _) = Setup();

        var result = await ctrl.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_OtherUsersItem_ReturnsNotFoundAndDoesNotRemove()
    {
        var (ctrl, db) = Setup("u1");
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "u2" });
        await db.SaveChangesAsync();

        var result = await ctrl.Delete(1);

        Assert.IsType<NotFoundResult>(result);
        Assert.Single(db.TodoItems);
    }
}
