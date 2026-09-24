
namespace LifeAdmin.Api.Models;
public class Subscription
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Data pierwszej płatności – od niej liczone są kolejne terminy.</summary>
    public DateTime StartDate { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "PLN";
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    /// <summary>Wstrzymana subskrypcja zostaje w historii, ale nie wlicza się do sum ani przypomnień.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Właściciel subskrypcji – każdy użytkownik widzi tylko swoje.</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }
}