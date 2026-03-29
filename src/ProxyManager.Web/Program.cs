using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProxyManager.Application;
using ProxyManager.Infrastructure;
using ProxyManager.Infrastructure.Identity;
using ProxyManager.Infrastructure.Persistence;
using ProxyManager.Web.Components.Auth;
using ProxyManager.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProxyManagerDbContext>();
    dbContext.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/auth/login", async (SignInManager<ApplicationUser> signInManager, [FromForm] string username, [FromForm] string password) =>
{
    var result = await signInManager.PasswordSignInAsync(username, password, isPersistent: true, lockoutOnFailure: true);
    if (result.Succeeded)
    {
        return Results.Redirect("/");
    }

    if (result.IsLockedOut)
    {
        return Results.Redirect("/login?error=locked");
    }

    return Results.Redirect("/login?error=invalid");
});

app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
