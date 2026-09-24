using System.Security.Claims;
using LifeAdmin.Api.Data;
using LifeAdmin.Api.Models;
using LifeAdmin.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LifeAdmin.Api.Endpoints;

public static class ReminderEndpoints
{
    public static IEndpointRouteBuilder MapReminderEndpoints(this IEndpointRouteBuilder app)
    {
        var reminders = app.MapGroup("/reminders").WithTags("Reminders").RequireAuthorization();

        // Lista przypomnień zalogowanego użytkownika (od startu aplikacji).
        reminders.MapGet("/", async (ReminderStore store, AppDbContext db, ClaimsPrincipal user) =>
            Results.Ok(await ForUser(store, db, user)));

        // Ręczne uruchomienie sprawdzenia (bez czekania na zadanie w tle).
        reminders.MapPost("/run", async (ReminderChecker checker, ReminderStore store, AppDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var sent = await checker.CheckAsync(ct);
            return Results.Ok(new { sent, items = await ForUser(store, db, user) });
        });

        return app;
    }

    private static async Task<IReadOnlyList<ReminderDto>> ForUser(ReminderStore store, AppDbContext db, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ids = await db.Subscriptions.Where(s => s.UserId == userId).Select(s => s.Id).ToListAsync();
        var set = ids.ToHashSet();
        return store.GetAll().Where(r => set.Contains(r.SubscriptionId)).ToList();
    }
}
