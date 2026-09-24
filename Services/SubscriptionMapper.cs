using LifeAdmin.Api.Models;

namespace LifeAdmin.Api.Services;

public static class SubscriptionMapper
{
    public static SubscriptionReadDto ToReadDto(this Subscription s, DateTime? today = null)
    {
        var reference = (today ?? DateTime.Today).Date;
        var next = SubscriptionDateCalculator.CalculateNextSubscriptionDate(s.StartDate, s.BillingCycle, reference);

        return new SubscriptionReadDto(
            s.Id,
            s.Name,
            s.StartDate,
            next,
            SubscriptionDateCalculator.DaysUntil(next, reference),
            s.Price,
            s.Currency,
            s.BillingCycle,
            s.IsActive);
    }
}
