using System.ComponentModel.DataAnnotations;

namespace LifeAdmin.Api.Models;

public record SubscriptionCreateDto(
    [property: Required, StringLength(100, MinimumLength = 1)] string Name,
    [property: Required] DateTime StartDate,
    [property: Range(0, 1_000_000)] decimal Price,
    [property: Required, StringLength(3, MinimumLength = 3)] string Currency,
    [property: Required, EnumDataType(typeof(BillingCycle))] BillingCycle BillingCycle,
    bool IsActive = true);

public record SubscriptionUpdateDto(
    [property: Required, StringLength(100, MinimumLength = 1)] string Name,
    [property: Required] DateTime StartDate,
    [property: Range(0, 1_000_000)] decimal Price,
    [property: Required, StringLength(3, MinimumLength = 3)] string Currency,
    [property: Required, EnumDataType(typeof(BillingCycle))] BillingCycle BillingCycle,
    bool IsActive = true);

public record SubscriptionStatusDto(bool IsActive);

public record SubscriptionReadDto(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime NextSubscriptionDate,
    int DaysUntilNext,
    decimal Price,
    string Currency,
    BillingCycle BillingCycle,
    bool IsActive);

public record ReminderDto(
    int SubscriptionId,
    string Name,
    DateTime DueDate,
    int DaysLeft,
    decimal Price,
    string Currency,
    DateTime CreatedAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
