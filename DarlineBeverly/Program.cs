
using DarlineBeverly.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DarlineBeverly.Data;
using DarlineBeverly.Dtos;
using DarlineBeverly.Models;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

// ----------------- TYPES -----------------
var builder = WebApplication.CreateBuilder(args);

// ----------------- SERVICES -----------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// HttpClient setup
builder.Services.AddHttpClient("Default", client =>
{
    client.BaseAddress = new Uri("http://localhost:5213"); // change if different
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

// Database
builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// File upload size limit
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200_000_000; // 200 MB
});

// Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// ----------------- MIGRATIONS -----------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
    db.Database.Migrate();
}

// ----------------- HELPERS -----------------
static string GenerateSlug(string title)
{
    var slug = title.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormKD);
    slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
    slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
    slug = Regex.Replace(slug, @"-+", "-");
    return slug;
}

// ----------------- BLOG API -----------------
app.MapGet("/api/blog/articles", async (BlogDbContext db, int page = 1, int pageSize = 10) =>
{
    var items = await db.Articles
        .Include(a => a.Category)
        .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
        .Where(a => a.IsPublished)
        .OrderByDescending(a => a.PublishedOn)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(a => new
        {
            a.Id,
            a.Title,
            a.Slug,
            a.Excerpt,
            a.PublishedOn,
            Category = a.Category != null ? a.Category.Name : null,
            Tags = a.ArticleTags.Select(t => t.Tag.Name)
        })
        .ToListAsync();

    return Results.Ok(items);
});

// ----------------- ADMIN API -----------------
app.MapGet("/api/admin/categories", async (BlogDbContext db) =>
{
    var categories = await db.Categories
        .Select(c => new { c.Id, c.Name })
        .ToListAsync();
    return Results.Ok(categories);
}).RequireAuthorization();

// This will match what your UI expects instead of /articleslist 404
app.MapGet("/api/admin/articleslist", async (BlogDbContext db) =>
{
    var articles = await db.Articles
        .Include(a => a.Category)
        .Select(a => new
        {
            a.Id,
            a.Title,
            a.Slug,
            a.IsPublished,
            Category = a.Category != null ? a.Category.Name : null
        })
        .ToListAsync();
    return Results.Ok(articles);
}).RequireAuthorization();

// ----------------- LOGIN -----------------
app.MapPost("/api/login", async (HttpContext context, LoginRequest login) =>
{
    if (login.Username == "admin" && login.Password == "password123")
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, login.Username)
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return Results.Ok(new { message = "Login successful" });
    }
    else
    {
        return Results.Unauthorized();
    }
});

// ----------------- MIDDLEWARE -----------------
if (!app.Environment.IsDevelopment()) // fixed from HostEnvironment
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapBlazorHub();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public record LoginRequest(string Username, string Password);
