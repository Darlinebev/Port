using DarlineBeverly.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DarlineBeverly.Data;
using DarlineBeverly.Dtos;
using DarlineBeverly.Models;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Components;


var builder = WebApplication.CreateBuilder(args);

// Add Razor Components & Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// HttpClient for API calls — base address from app settings
builder.Services.AddHttpClient("Default", (sp, client) =>
{
    // Use the app's own base address in dev
    var nav = sp.GetRequiredService<NavigationManager>();
    client.BaseAddress = new Uri(nav.BaseUri);
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

// Database
builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// File upload size limit
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200_000_000; // 200MB
});

var app = builder.Build();

// Run migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
    db.Database.Migrate();
}

// Slug generator
static string GenerateSlug(string title)
{
    var slug = title.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormKD);
    slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
    slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
    slug = Regex.Replace(slug, @"-+", "-");
    return slug;
}

// --- Public endpoints ---
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

// --- Admin endpoints ---
app.MapGet("/api/admin/categories", async (BlogDbContext db) =>
{
    var categories = await db.Categories
        .Select(c => new { c.Id, c.Name })
        .ToListAsync();
    return Results.Ok(categories);
})
.RequireAuthorization();

// Create article
app.MapPost("/api/admin/articles", async (BlogDbContext db, ArticleDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Title)) return Results.BadRequest("Title required");

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
})
.RequireAuthorization();

// Update
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
})
.RequireAuthorization();

// Delete
app.MapDelete("/api/admin/articles/{id}", async (int id, BlogDbContext db) =>
{
    var article = await db.Articles.FindAsync(id);
    if (article is null) return Results.NotFound();
    db.Articles.Remove(article);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireAuthorization();

// File upload
app.MapPost("/api/admin/upload", async (HttpRequest request, IWebHostEnvironment env) =>
{
    if (!request.HasFormContentType) return Results.BadRequest("Expected form file");
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0) return Results.BadRequest("No file uploaded");

    var uploads = Path.Combine(env.WebRootPath, "uploads");
    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

    var unique = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
    var filePath = Path.Combine(uploads, unique);
    using (var fs = File.Create(filePath))
    {
        await file.CopyToAsync(fs);
    }

    var url = $"/uploads/{unique}";
    return Results.Ok(new { url, fileName = file.FileName, size = file.Length, contentType = file.ContentType });
})
.RequireAuthorization();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapBlazorHub();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
