using System.Security.Claims;
using LifeAdmin.Api.Data;
using LifeAdmin.Api.Models;
using LifeAdmin.Api.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LifeAdmin.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/register", Register);
        auth.MapPost("/login", Login);
        auth.MapPost("/logout", (Delegate)Logout);
        auth.MapGet("/me", Me).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Register(AuthRequest input, AppDbContext db, IPasswordHasher<User> hasher, HttpContext http)
    {
        if (!MiniValidator.TryValidate(input, out var errors))
            return Results.ValidationProblem(errors);

        var username = input.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Username == username))
            return Results.Conflict(new { error = "Taki użytkownik już istnieje." });

        var user = new User { Username = username };
        user.PasswordHash = hasher.HashPassword(user, input.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await SignInAsync(http, user);
        return Results.Ok(new UserInfoDto(user.Id, user.Username));
    }

    private static async Task<IResult> Login(AuthRequest input, AppDbContext db, IPasswordHasher<User> hasher, HttpContext http)
    {
        var username = input.Username?.Trim() ?? "";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

        // Ten sam komunikat dla złego loginu i hasła – nie zdradzamy, które konto istnieje.
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password ?? "") == PasswordVerificationResult.Failed)
            return Results.Json(new { error = "Nieprawidłowy login lub hasło." }, statusCode: StatusCodes.Status401Unauthorized);

        await SignInAsync(http, user);
        return Results.Ok(new UserInfoDto(user.Id, user.Username));
    }

    private static async Task<IResult> Logout(HttpContext http)
    {
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static IResult Me(ClaimsPrincipal user) =>
        Results.Ok(new UserInfoDto(
            int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!),
            user.Identity!.Name!));

    private static Task SignInAsync(HttpContext http, User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }
}
