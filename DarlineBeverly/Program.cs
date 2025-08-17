using DarlineBeverly.Components;
using DarlineBeverly.Data;
using DarlineBeverly.Dtos;
using DarlineBeverly.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntiforgery();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("Default", client =>
{
    client.BaseAddress = new Uri("http://localhost:5213");
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200_000_000; // 200 MB
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
    db.Database.Migrate();
}

// Helper
static string GenerateSlug(string title)
{
    var slug = title.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormKD);
    slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
    slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
    slug = Regex.Replace(slug, @"-+", "-");
    return slug;
}

// Blog API Endpoints
app.MapGet("/api/blog/articles", async (BlogDbContext db, int page = 1, int pageSize = 10, string? search = null, int? categoryId = null, string? tag = null) =>
{
    var q = db.Articles
        .Include(a => a.Category)
        .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
        .Where(a => a.IsPublished)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(a => a.Title.Contains(search) || a.Content.Contains(search));
    if (categoryId.HasValue)
        q = q.Where(a => a.CategoryId == categoryId.Value);
    if (!string.IsNullOrWhiteSpace(tag))
        q = q.Where(a => a.ArticleTags.Any(at => at.Tag.Name == tag));

    var items = await q.OrderByDescending(a => a.PublishedOn)
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

app.MapGet("/api/blog/articles/{slug}", async (BlogDbContext db, string slug) =>
{
    var article = await db.Articles
        .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
        .Include(a => a.Category)
        .Include(a => a.Files)
        .FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished);

    return article is null ? Results.NotFound() : Results.Ok(article);
});

// Admin API Endpoints
app.MapGet("/api/admin/articleslist", async (BlogDbContext db) =>
{
    var articles = await db.Articles
        .Select(a => new { a.Id, a.Title, a.IsPublished })
        .ToListAsync();
    return Results.Ok(articles);
}).RequireAuthorization().DisableAntiforgery();

app.MapGet("/api/admin/categories", async (BlogDbContext db) =>
{
    var categories = await db.Categories
        .Select(c => new { c.Id, c.Name })
        .ToListAsync();
    return Results.Ok(categories);
}).RequireAuthorization().DisableAntiforgery();

app.MapPost("/api/admin/articles", async (BlogDbContext db, ArticleDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Title))
        return Results.BadRequest("Title required");

    var slug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Title) : GenerateSlug(dto.Slug);
    var baseSlug = slug;
    int i = 1;
    while (await db.Articles.AnyAsync(a => a.Slug == slug))
    {
        slug = $"{baseSlug}-{i++}";
    }
    var article = new Article
    {
        Title = dto.Title,
        Slug = slug,
        Content = dto.Content,
        Excerpt = dto.Excerpt,
        IsPublished = dto.IsPublished,
        PublishedOn = dto.IsPublished ? (dto.PublishedOn ?? DateTime.UtcNow) : null,
        CategoryId = dto.CategoryId
    };
    foreach (var tname in dto.TagNames.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == tname) ?? new Tag { Name = tname };
        if (tag.Id == 0) db.Tags.Add(tag);
        article.ArticleTags.Add(new ArticleTag { Tag = tag });
    }
    db.Articles.Add(article);
    await db.SaveChangesAsync();
    return Results.Created($"/api/blog/articles/{article.Slug}", article);
}).RequireAuthorization().DisableAntiforgery();

app.MapPut("/api/admin/articles/{id}", async (int id, BlogDbContext db, ArticleDto dto) =>
{
    var article = await db.Articles
        .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (article is null) return Results.NotFound();

    article.Title = dto.Title;
    article.Content = dto.Content;
    article.Excerpt = dto.Excerpt;
    article.IsPublished = dto.IsPublished;
    article.PublishedOn = dto.IsPublished ? (dto.PublishedOn ?? DateTime.UtcNow) : null;
    article.CategoryId = dto.CategoryId;
    article.ArticleTags.Clear();
    foreach (var tname in dto.TagNames.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == tname) ?? new Tag { Name = tname };
        if (tag.Id == 0) db.Tags.Add(tag);
        article.ArticleTags.Add(new ArticleTag { Tag = tag });
    }
    await db.SaveChangesAsync();
    return Results.Ok(article);
}).RequireAuthorization().DisableAntiforgery();

app.MapDelete("/api/admin/articles/{id}", async (int id, BlogDbContext db) =>
{
    var article = await db.Articles.FindAsync(id);
    if (article is null) return Results.NotFound();

    db.Articles.Remove(article);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization().DisableAntiforgery();

app.MapPost("/api/admin/upload", async (HttpRequest request, IWebHostEnvironment env) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected form file");
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file uploaded");

    var uploads = Path.Combine(env.WebRootPath, "uploads");
    if (!Directory.Exists(uploads))
        Directory.CreateDirectory(uploads);
    var unique = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
    var filePath = Path.Combine(uploads, unique);
    using (var fs = File.Create(filePath))
    {
        await file.CopyToAsync(fs);
    }
    var url = $"/uploads/{unique}";
    return Results.Ok(new { url, fileName = file.FileName, size = file.Length, contentType = file.ContentType });
}).RequireAuthorization().DisableAntiforgery();

// Login Endpoint - No .DisableAntiforgery() here
app.MapPost("/api/login", async (HttpContext context, LoginRequest login, IAntiforgery antiforgery) =>
{
    // Validate the antiforgery token manually because this is a minimal API endpoint.
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("Invalid antiforgery token");
    }

    Console.WriteLine($"Username: {login.Username}, Password: {login.Password}");

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


// Middleware
if (!app.Environment.IsDevelopment())
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

