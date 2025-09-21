using DarlineBeverly.Data;
using DarlineBeverly.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>();


builder.Services.AddAuthentication(AuthConstant.Scheme)
    .AddCookie(AuthConstant.Scheme, options =>
    {
        options.Cookie.Name = AuthConstant.CookieName;
        options.LoginPath = "/authentication/login";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
        
    });







var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization(); 
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery(); 

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();




app.Run();
