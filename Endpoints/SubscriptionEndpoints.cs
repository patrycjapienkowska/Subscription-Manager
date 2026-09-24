using System.Security.Claims;
using LifeAdmin.Api.Data;
using LifeAdmin.Api.Models;
using LifeAdmin.Api.Services;
using LifeAdmin.Api.Validation;
using Microsoft.EntityFrameworkCore;

namespace LifeAdmin.Api.Endpoints;

public static class SubscriptionEndpoints
{
    private static int GetUserId(ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Tylko subskrypcje zalogowanego użytkownika.</summary>
    private static IQueryable<Subscription> Mine(AppDbContext db, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        return db.Subscriptions.Where(s => s.UserId == userId);
    }

    private static Task<Subscription?> FindMine(AppDbContext db, ClaimsPrincipal user, int id) =>
        Mine(db, user).FirstOrDefaultAsync(s => s.Id == id);

    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var subs = app.MapGroup("/subscriptions").WithTags("Subscriptions").RequireAuthorization();

        subs.MapGet("/", GetAll);
        subs.MapGet("/{id:int}", GetById);
        subs.MapGet("/upcoming", GetUpcoming);
        subs.MapPost("/", Create);
        subs.MapPut("/{id:int}", Update);
        subs.MapPatch("/{id:int}/status", SetStatus);
        subs.MapDelete("/{id:int}", Delete);

        return app;
    }

    // sort: startDate | name | id  (prefiks "-" = malejąco); "dueDate" działa jako alias startDate
    private static async Task<IResult> GetAll(
        AppDbContext db,
        ClaimsPrincipal user,
        string? search = null,
        string? sort = "startDate",
        bool? active = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var query = Mine(db, user);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => EF.Functions.Like(s.Name, $"%{search}%"));

        if (active is not null)
            query = query.Where(s => s.IsActive == active);

        var desc = sort?.StartsWith('-') == true;
        var key = (desc ? sort![1..] : sort ?? "startDate").ToLowerInvariant();
        query = (key, desc) switch
        {
            ("name", false) => query.OrderBy(s => s.Name),
            ("name", true) => query.OrderByDescending(s => s.Name),
            ("id", false) => query.OrderBy(s => s.Id),
            ("id", true) => query.OrderByDescending(s => s.Id),
            (_, true) => query.OrderByDescending(s => s.StartDate),
            _ => query.OrderBy(s => s.StartDate),
        };

        var total = await query.CountAsync();
        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var today = DateTime.Today;
        var items = entities.Select(s => s.ToReadDto(today)).ToList();

        return Results.Ok(new PagedResult<SubscriptionReadDto>(items, page, pageSize, total));
    }

    private static async Task<IResult> GetById(int id, AppDbContext db, ClaimsPrincipal user) =>
        await FindMine(db, user, id) is Subscription s
            ? Results.Ok(s.ToReadDto())
            : Results.NotFound();

    // Aktywne subskrypcje, których najbliższy termin wypada w ciągu `days` dni.
    private static async Task<IResult> GetUpcoming(AppDbContext db, ClaimsPrincipal user, int days = 7)
    {
        days = Math.Clamp(days, 1, 365);
        var today = DateTime.Today;
        var until = today.AddDays(days);

        var active = await Mine(db, user).Where(s => s.IsActive).ToListAsync();
        var items = active
            .Select(s => s.ToReadDto(today))
            .Where(d => d.NextSubscriptionDate <= until)
            .OrderBy(d => d.NextSubscriptionDate)
            .ToList();

        return Results.Ok(new
        {
            from = today,
            to = until,
            days,
            count = items.Count,
            items
        });
    }

    private static async Task<IResult> Create(SubscriptionCreateDto input, AppDbContext db, ClaimsPrincipal user)
    {
        if (!MiniValidator.TryValidate(input, out var errors))
            return Results.ValidationProblem(errors);

        var entity = new Subscription
        {
            UserId = GetUserId(user),
            Name = input.Name.Trim(),
            StartDate = input.StartDate.Date,
            Price = input.Price,
            Currency = input.Currency.ToUpperInvariant(),
            BillingCycle = input.BillingCycle,
            IsActive = input.IsActive
        };

        db.Subscriptions.Add(entity);
        await db.SaveChangesAsync();
        return Results.Created($"/subscriptions/{entity.Id}", entity.ToReadDto());
    }

    private static async Task<IResult> Update(int id, SubscriptionUpdateDto input, AppDbContext db, ClaimsPrincipal user)
    {
        if (!MiniValidator.TryValidate(input, out var errors))
            return Results.ValidationProblem(errors);

        var existing = await FindMine(db, user, id);
        if (existing is null) return Results.NotFound();

        existing.Name = input.Name.Trim();
        existing.StartDate = input.StartDate.Date;
        existing.Price = input.Price;
        existing.Currency = input.Currency.ToUpperInvariant();
        existing.BillingCycle = input.BillingCycle;
        existing.IsActive = input.IsActive;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // Szybkie wstrzymanie / wznowienie bez przesyłania całego obiektu.
    private static async Task<IResult> SetStatus(int id, SubscriptionStatusDto input, AppDbContext db, ClaimsPrincipal user)
    {
        var existing = await FindMine(db, user, id);
        if (existing is null) return Results.NotFound();

        existing.IsActive = input.IsActive;
        await db.SaveChangesAsync();
        return Results.Ok(existing.ToReadDto());
    }

    private static async Task<IResult> Delete(int id, AppDbContext db, ClaimsPrincipal user)
    {
        var existing = await FindMine(db, user, id);
        if (existing is null) return Results.NotFound();

        db.Subscriptions.Remove(existing);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
