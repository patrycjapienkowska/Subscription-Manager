using System.ComponentModel.DataAnnotations;

namespace LifeAdmin.Api.Validation;

/// <summary>Prosty walidator DataAnnotations bez zewnętrznych pakietów.</summary>
public static class MiniValidator
{
    public static bool TryValidate(object model, out Dictionary<string, string[]> errors)
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var ok = Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        errors = results
            .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : new[] { "" })
                .Select(m => (Member: m, Msg: r.ErrorMessage ?? "Invalid")))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Msg).ToArray());
        return ok;
    }
}
