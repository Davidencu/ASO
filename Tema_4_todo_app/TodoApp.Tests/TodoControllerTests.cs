using Microsoft.AspNetCore.Mvc;
using TodoApp.Controllers;
using TodoApp.Data;
using TodoApp.Models;
using TodoApp.Tests.Helpers;

namespace TodoApp.Tests;

public class TodoControllerTests
{
    private static (TodoController ctrl, AppDbContext db) Setup(string userId = "user-1")
    {
        var db = TestHelpers.CreateDb();
        return (new TodoController(db).WithUser(userId), db);
    }

    // ── Index ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsOnlyCurrentUserItems()
    {
        var (ctrl, db) = Setup("u1");
        db.TodoItems.AddRange(
            new TodoItem { Title = "Mine",  UserId = "u1" },
            new TodoItem { Title = "Other", UserId = "u2" });
        await db.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await ctrl.Index(null));
        var items = Assert.IsAssignableFrom<IList<TodoItem>>(view.Model);

        Assert.Single(items);
        Assert.Equal("Mine", items[0].Title);
    }

    [Fact]
    public async Task Index_FilterActive_ExcludesCompleted()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.AddRange(
            new TodoItem { Title = "Active",    UserId = "user-1", IsCompleted = false },
            new TodoItem { Title = "Completed", UserId = "user-1", IsCompleted = true  });
        await db.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await ctrl.Index("active"));
        var items = Assert.IsAssignableFrom<IList<TodoItem>>(view.Model);

        Assert.Single(items);
        Assert.False(items[0].IsCompleted);
    }

    [Fact]
    public async Task Index_FilterCompleted_ExcludesActive()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.AddRange(
            new TodoItem { Title = "Active",    UserId = "user-1", IsCompleted = false },
            new TodoItem { Title = "Completed", UserId = "user-1", IsCompleted = true  });
        await db.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await ctrl.Index("completed"));
        var items = Assert.IsAssignableFrom<IList<TodoItem>>(view.Model);

        Assert.Single(items);
        Assert.True(items[0].IsCompleted);
    }

    [Fact]
    public async Task Index_UnknownFilter_ReturnsAllUserItems()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.AddRange(
            new TodoItem { Title = "A", UserId = "user-1", IsCompleted = false },
            new TodoItem { Title = "B", UserId = "user-1", IsCompleted = true  });
        await db.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await ctrl.Index("whatever"));
        var items = Assert.IsAssignableFrom<IList<TodoItem>>(view.Model);

        Assert.Equal(2, items.Count);
    }

    // ── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidItem_SavesWithUserIdAndRedirects()
    {
        var (ctrl, db) = Setup();

        var result = await ctrl.Create(new TodoItem { Title = "Buy milk" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TodoController.Index), redirect.ActionName);
        var saved = Assert.Single(db.TodoItems);
        Assert.Equal("Buy milk", saved.Title);
        Assert.Equal("user-1", saved.UserId);
    }

    [Fact]
    public async Task Create_InvalidModel_DoesNotPersist()
    {
        var (ctrl, db) = Setup();
        ctrl.ModelState.AddModelError("Title", "Required");

        await ctrl.Create(new TodoItem { Title = "" });

        Assert.Empty(db.TodoItems);
    }

    // ── Toggle ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Toggle_ExistingItem_FlipsIsCompleted()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "user-1", IsCompleted = false });
        await db.SaveChangesAsync();

        await ctrl.Toggle(1);

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
    public async Task Toggle_OtherUsersItem_ReturnsNotFound()
    {
        var (ctrl, db) = Setup("u1");
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "u2" });
        await db.SaveChangesAsync();

        var result = await ctrl.Toggle(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Toggle_MissingId_ReturnsNotFound()
    {
        var (ctrl, _) = Setup();

        var result = await ctrl.Toggle(999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingItem_RemovesAndRedirects()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "user-1" });
        await db.SaveChangesAsync();

        var result = await ctrl.Delete(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(db.TodoItems);
    }

    [Fact]
    public async Task Delete_MissingItem_StillRedirects()
    {
        var (ctrl, _) = Setup();

        var result = await ctrl.Delete(999);

        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public async Task Delete_OtherUsersItem_DoesNotRemove()
    {
        var (ctrl, db) = Setup("u1");
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "u2" });
        await db.SaveChangesAsync();

        await ctrl.Delete(1);

        Assert.Single(db.TodoItems);
    }

    // ── Edit ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Edit_Get_ReturnsViewWithCorrectItem()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "user-1" });
        await db.SaveChangesAsync();

        var result = await ctrl.Edit(1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TodoItem>(view.Model);
        Assert.Equal(1, model.Id);
    }

    [Fact]
    public async Task Edit_Get_OtherUsersItem_ReturnsNotFound()
    {
        var (ctrl, db) = Setup("u1");
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "u2" });
        await db.SaveChangesAsync();

        var result = await ctrl.Edit(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_MissingItem_ReturnsNotFound()
    {
        var (ctrl, _) = Setup();

        var result = await ctrl.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ValidItem_UpdatesAndRedirects()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "Old", UserId = "user-1" });
        await db.SaveChangesAsync();

        var result = await ctrl.Edit(new TodoItem { Id = 1, Title = "New", Priority = Priority.High });

        Assert.IsType<RedirectToActionResult>(result);
        var saved = db.TodoItems.Single();
        Assert.Equal("New", saved.Title);
        Assert.Equal(Priority.High, saved.Priority);
    }

    [Fact]
    public async Task Edit_Post_InvalidModel_ReturnsView()
    {
        var (ctrl, db) = Setup();
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "user-1" });
        await db.SaveChangesAsync();
        ctrl.ModelState.AddModelError("Title", "Required");

        var result = await ctrl.Edit(new TodoItem { Id = 1 });

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_Post_OtherUsersItem_ReturnsNotFound()
    {
        var (ctrl, db) = Setup("u1");
        db.TodoItems.Add(new TodoItem { Id = 1, Title = "T", UserId = "u2" });
        await db.SaveChangesAsync();

        var result = await ctrl.Edit(new TodoItem { Id = 1, Title = "Hacked" });

        Assert.IsType<NotFoundResult>(result);
    }
}
