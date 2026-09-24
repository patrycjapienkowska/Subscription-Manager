using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LifeAdmin.Api.Models;
using LifeAdmin.Api.Data;
using LifeAdmin.Api.Endpoints;
using LifeAdmin.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Logowanie (cookie)
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "LifeAdmin.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
        // API ma zwracać 401/403 zamiast przekierowania na stronę logowania.
        o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization();

// Przypomnienia
builder.Services.Configure<ReminderOptions>(builder.Configuration.GetSection(ReminderOptions.SectionName));
builder.Services.AddSingleton<ReminderStore>();
builder.Services.AddSingleton<IReminderNotifier, LoggingReminderNotifier>();
builder.Services.AddScoped<ReminderChecker>();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddHostedService<SubscriptionReminderService>();
}

var app = builder.Build();

// W Dockerze baza startuje pusta – automatycznie aplikujemy migracje.
if (app.Environment.IsEnvironment("Docker"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "LifeAdmin API is running");
app.MapGet("/ui", () => Results.Redirect("/ui/index.html"));

app.MapAuthEndpoints();
app.MapSubscriptionEndpoints();
app.MapReminderEndpoints();

app.Run();

public partial class Program { }
