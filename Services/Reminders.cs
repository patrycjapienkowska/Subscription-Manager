using System.Collections.Concurrent;
using LifeAdmin.Api.Data;
using LifeAdmin.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LifeAdmin.Api.Services;

/// <summary>Ustawienia przypomnień (sekcja "Reminders" w appsettings.json).</summary>
public class ReminderOptions
{
    public const string SectionName = "Reminders";

    /// <summary>Ile dni przed terminem przypominać.</summary>
    public int DaysAhead { get; set; } = 3;

    /// <summary>Co ile godzin sprawdzać subskrypcje.</summary>
    public double CheckIntervalHours { get; set; } = 24;
}

/// <summary>
/// Kanał wysyłki przypomnień. Domyślnie log + lista w pamięci;
/// żeby wysyłać e-maile, wystarczy dodać implementację (np. SMTP) i podmienić rejestrację w Program.cs.
/// </summary>
public interface IReminderNotifier
{
    Task NotifyAsync(ReminderDto reminder, CancellationToken ct);
}

/// <summary>Przechowuje ostatnie przypomnienia w pamięci (do podglądu w GUI).</summary>
public class ReminderStore
{
    private const int MaxItems = 100;
    private readonly ConcurrentDictionary<string, ReminderDto> _items = new();

    private static string Key(int subscriptionId, DateTime dueDate) => $"{subscriptionId}:{dueDate:yyyy-MM-dd}";

    public bool WasSent(int subscriptionId, DateTime dueDate) => _items.ContainsKey(Key(subscriptionId, dueDate));

    public void Add(ReminderDto reminder)
    {
        _items[Key(reminder.SubscriptionId, reminder.DueDate)] = reminder;

        if (_items.Count <= MaxItems) return;
        foreach (var old in _items.OrderBy(x => x.Value.CreatedAt).Take(_items.Count - MaxItems))
            _items.TryRemove(old.Key, out _);
    }

    public IReadOnlyList<ReminderDto> GetAll() =>
        _items.Values.OrderBy(r => r.DueDate).ThenBy(r => r.Name).ToList();
}

public class LoggingReminderNotifier(ReminderStore store, ILogger<LoggingReminderNotifier> logger) : IReminderNotifier
{
    public Task NotifyAsync(ReminderDto reminder, CancellationToken ct)
    {
        logger.LogInformation(
            "Przypomnienie: '{Name}' - termin {DueDate:yyyy-MM-dd} (za {DaysLeft} dni), {Price} {Currency}",
            reminder.Name, reminder.DueDate, reminder.DaysLeft, reminder.Price, reminder.Currency);

        store.Add(reminder);
        return Task.CompletedTask;
    }
}

/// <summary>Jednorazowe sprawdzenie subskrypcji i wysłanie przypomnień.</summary>
public class ReminderChecker(
    AppDbContext db,
    IReminderNotifier notifier,
    ReminderStore store,
    IOptions<ReminderOptions> options)
{
    public async Task<int> CheckAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var daysAhead = Math.Max(0, options.Value.DaysAhead);

        var active = await db.Subscriptions
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var sent = 0;
        foreach (var dto in active.Select(s => s.ToReadDto(today)))
        {
            if (dto.DaysUntilNext < 0 || dto.DaysUntilNext > daysAhead) continue;
            if (store.WasSent(dto.Id, dto.NextSubscriptionDate)) continue;

            await notifier.NotifyAsync(new ReminderDto(
                dto.Id,
                dto.Name,
                dto.NextSubscriptionDate,
                dto.DaysUntilNext,
                dto.Price,
                dto.Currency,
                DateTime.Now), ct);
            sent++;
        }

        return sent;
    }
}

/// <summary>Zadanie w tle: sprawdza subskrypcje od razu po starcie, a potem cyklicznie.</summary>
public class SubscriptionReminderService(
    IServiceScopeFactory scopeFactory,
    IOptions<ReminderOptions> options,
    ILogger<SubscriptionReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(0.01, options.Value.CheckIntervalHours));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var checker = scope.ServiceProvider.GetRequiredService<ReminderChecker>();
                var sent = await checker.CheckAsync(stoppingToken);
                logger.LogInformation("Sprawdzono subskrypcje, nowych przypomnień: {Count}", sent);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Błąd (np. baza niedostępna) nie może zabić całej aplikacji.
                logger.LogError(ex, "Błąd podczas sprawdzania przypomnień");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
