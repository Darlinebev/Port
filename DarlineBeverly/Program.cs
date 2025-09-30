
using DarlineBeverly.Data;
using DarlineBeverly.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Http;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Use SQLite for persistence
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity (with default cookie scheme: Identity.Application)
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false; // optional
    })
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/authentication/login";       // your login page
    options.LogoutPath = "/authentication/logout";     // your logout page
    options.AccessDeniedPath = "/authentication/access-denied"; // optional
});

builder.Services.AddServerSideBlazor()
       .AddCircuitOptions(o => o.DetailedErrors = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.MapGet("/api/graphics-images", () =>
{
    var folderPath = Path.Combine(app.Environment.WebRootPath, "images", "graphics");
    var files = Directory.GetFiles(folderPath)
                         .Select(Path.GetFileName)           // just file names
                         .Select(name => $"/images/graphics/{name}") // convert to URL
                         .ToList();
    return Results.Json(files);
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();  // must come before Authorization
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
