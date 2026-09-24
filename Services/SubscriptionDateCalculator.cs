using LifeAdmin.Api.Models;

namespace LifeAdmin.Api.Services;

/// <summary>
/// Wylicza terminy subskrypcji na podstawie daty startu i cyklu rozliczeń.
/// </summary>
public static class SubscriptionDateCalculator
{
    /// <summary>
    /// Zwraca pierwszy termin cyklu przypadający dziś lub w przyszłości.
    /// </summary>
    public static DateTime CalculateNextSubscriptionDate(DateTime startDate, BillingCycle cycle, DateTime? fromDate = null)
    {
        var start = startDate.Date;
        var reference = (fromDate ?? DateTime.Today).Date;

        if (start >= reference)
            return start;

        if (cycle == BillingCycle.Weekly)
        {
            var weeks = (int)Math.Ceiling((reference - start).TotalDays / 7.0);
            return start.AddDays(weeks * 7);
        }

        var monthsPerCycle = cycle switch
        {
            BillingCycle.Quarterly => 3,
            BillingCycle.Yearly => 12,
            _ => 1
        };

        // Liczymy zawsze od daty startu (start + n * cykl), żeby uniknąć "dryfu"
        // przy końcówkach miesiąca, np. 31.01 -> 28.02 -> 31.03 zamiast 28.03.
        var n = 1;
        var next = start.AddMonths(monthsPerCycle);
        while (next < reference)
        {
            n++;
            next = start.AddMonths(monthsPerCycle * n);
        }

        return next;
    }

    public static int DaysUntil(DateTime date, DateTime? fromDate = null)
        => (int)(date.Date - (fromDate ?? DateTime.Today).Date).TotalDays;
}
