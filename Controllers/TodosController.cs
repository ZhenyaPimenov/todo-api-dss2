using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/todos")]
public class TodosController : ControllerBase
{
    private readonly AppDbContext _context;

    public TodosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicTodos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string status = "all",
        [FromQuery] string? priority = null,
        [FromQuery] DateOnly? dueFrom = null,
        [FromQuery] DateOnly? dueTo = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDir = "desc",
        [FromQuery] string? search = null)
    {
        var validationResult = ValidateListQuery(page, pageSize, status, priority, sortBy, sortDir, search);
        if (validationResult is not null)
        {
            return validationResult;
        }

        IQueryable<TodoItem> query = _context.Todos.AsNoTracking().Where(x => x.IsPublic);
        query = ApplyListFilters(query, status, priority, dueFrom, dueTo, search);
        query = ApplySorting(query, sortBy, sortDir);

        return Ok(await ToPagedResponseAsync(query, page, pageSize));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetOwnTodos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string status = "all",
        [FromQuery] string? priority = null,
        [FromQuery] DateOnly? dueFrom = null,
        [FromQuery] DateOnly? dueTo = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDir = "desc",
        [FromQuery] string? search = null)
    {
        var validationResult = ValidateListQuery(page, pageSize, status, priority, sortBy, sortDir, search);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var userId = GetCurrentUserId();

        IQueryable<TodoItem> query = _context.Todos.AsNoTracking().Where(x => x.UserId == userId);
        query = ApplyListFilters(query, status, priority, dueFrom, dueTo, search);
        query = ApplySorting(query, sortBy, sortDir);

        return Ok(await ToPagedResponseAsync(query, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetTodoById(Guid id)
    {
        var userId = GetCurrentUserId();
        var todo = await _context.Todos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (todo is null)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Not Found", "Todo was not found."));
        }

        if (todo.UserId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CreateProblem(StatusCodes.Status403Forbidden, "Forbidden", "You do not have access to this todo."));
        }

        return Ok(MapTodo(todo));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTodo([FromBody] CreateTodoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = GetCurrentUserId();

        var todo = new TodoItem
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim(),
            Priority = Enum.Parse<TodoPriority>(request.Priority, true),
            DueDate = request.DueDate,
            IsPublic = request.IsPublic,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTodoById), new { id = todo.Id }, MapTodo(todo));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateTodo(Guid id, [FromBody] UpdateTodoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = GetCurrentUserId();
        var todo = await _context.Todos.FirstOrDefaultAsync(x => x.Id == id);

        if (todo is null)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Not Found", "Todo was not found."));
        }

        if (todo.UserId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CreateProblem(StatusCodes.Status403Forbidden, "Forbidden", "You do not have access to this todo."));
        }

        todo.Title = request.Title.Trim();
        todo.Details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim();
        todo.Priority = Enum.Parse<TodoPriority>(request.Priority, true);
        todo.DueDate = request.DueDate;
        todo.IsPublic = request.IsPublic;
        todo.IsCompleted = request.IsCompleted;
        todo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapTodo(todo));
    }

    [HttpPatch("{id:guid}/completion")]
    [Authorize]
    public async Task<IActionResult> SetCompletion(Guid id, [FromBody] SetCompletionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userId = GetCurrentUserId();
        var todo = await _context.Todos.FirstOrDefaultAsync(x => x.Id == id);

        if (todo is null)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Not Found", "Todo was not found."));
        }

        if (todo.UserId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CreateProblem(StatusCodes.Status403Forbidden, "Forbidden", "You do not have access to this todo."));
        }

        todo.IsCompleted = request.IsCompleted!.Value;
        todo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(MapTodo(todo));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteTodo(Guid id)
    {
        var userId = GetCurrentUserId();
        var todo = await _context.Todos.FirstOrDefaultAsync(x => x.Id == id);

        if (todo is null)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Not Found", "Todo was not found."));
        }

        if (todo.UserId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CreateProblem(StatusCodes.Status403Forbidden, "Forbidden", "You do not have access to this todo."));
        }

        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var rawValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
        return Guid.Parse(rawValue ?? throw new UnauthorizedAccessException("Missing user id claim."));
    }

    private IActionResult? ValidateListQuery(int page, int pageSize, string status, string? priority, string sortBy, string sortDir, string? search)
    {
        ModelState.Clear();

        if (page < 1)
        {
            ModelState.AddModelError(nameof(page), "Page must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > 50)
        {
            ModelState.AddModelError(nameof(pageSize), "PageSize must be between 1 and 50.");
        }

        var allowedStatus = new[] { "all", "active", "completed" };
        if (!allowedStatus.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(status), "Status must be one of: all, active, completed.");
        }

        if (!string.IsNullOrWhiteSpace(priority) && !Enum.TryParse<TodoPriority>(priority, true, out _))
        {
            ModelState.AddModelError(nameof(priority), "Priority must be one of: low, medium, high.");
        }

        var allowedSortBy = new[] { "createdAt", "dueDate", "priority", "title" };
        if (!allowedSortBy.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(sortBy), "SortBy must be one of: createdAt, dueDate, priority, title.");
        }

        var allowedSortDir = new[] { "asc", "desc" };
        if (!allowedSortDir.Contains(sortDir, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(sortDir), "SortDir must be one of: asc, desc.");
        }

        if (search is not null && search.Length > 100)
        {
            ModelState.AddModelError(nameof(search), "Search must be 100 characters or fewer.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return null;
    }

    private static IQueryable<TodoItem> ApplyListFilters(
        IQueryable<TodoItem> query,
        string status,
        string? priority,
        DateOnly? dueFrom,
        DateOnly? dueTo,
        string? search)
    {
        if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => !x.IsCompleted);
        }
        else if (status.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.IsCompleted);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            var parsedPriority = Enum.Parse<TodoPriority>(priority, true);
            query = query.Where(x => x.Priority == parsedPriority);
        }

        if (dueFrom.HasValue)
        {
            query = query.Where(x => x.DueDate.HasValue && x.DueDate.Value >= dueFrom.Value);
        }

        if (dueTo.HasValue)
        {
            query = query.Where(x => x.DueDate.HasValue && x.DueDate.Value <= dueTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                (x.Details != null && x.Details.ToLower().Contains(term)));
        }

        return query;
    }

    private static IQueryable<TodoItem> ApplySorting(IQueryable<TodoItem> query, string sortBy, string sortDir)
    {
        var ascending = sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "duedate" => ascending ? query.OrderBy(x => x.DueDate).ThenBy(x => x.CreatedAt) : query.OrderByDescending(x => x.DueDate).ThenByDescending(x => x.CreatedAt),
            "priority" => ascending ? query.OrderBy(x => x.Priority).ThenBy(x => x.CreatedAt) : query.OrderByDescending(x => x.Priority).ThenByDescending(x => x.CreatedAt),
            "title" => ascending ? query.OrderBy(x => x.Title).ThenBy(x => x.CreatedAt) : query.OrderByDescending(x => x.Title).ThenByDescending(x => x.CreatedAt),
            _ => ascending ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt)
        };
    }

    private static TodoResponse MapTodo(TodoItem todo) => new()
    {
        Id = todo.Id,
        Title = todo.Title,
        Details = todo.Details,
        Priority = todo.Priority.ToString().ToLowerInvariant(),
        DueDate = todo.DueDate,
        IsCompleted = todo.IsCompleted,
        IsPublic = todo.IsPublic,
        CreatedAt = todo.CreatedAt,
        UpdatedAt = todo.UpdatedAt
    };

    private async Task<PagedResponse<TodoResponse>> ToPagedResponseAsync(IQueryable<TodoItem> query, int page, int pageSize)
    {
        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TodoResponse
            {
                Id = x.Id,
                Title = x.Title,
                Details = x.Details,
                Priority = x.Priority.ToString().ToLower(),
                DueDate = x.DueDate,
                IsCompleted = x.IsCompleted,
                IsPublic = x.IsPublic,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new PagedResponse<TodoResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail) => new()
    {
        Type = $"https://httpstatuses.com/{status}",
        Title = title,
        Status = status,
        Detail = detail
    };
}
