using Blazeditor.Application.Components;
using Blazeditor.Application.Data;
using Blazeditor.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<SimpleAuthService>();
builder.Services.AddScoped<DefinitionSerializerService>();
builder.Services.AddScoped<DefinitionManager>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<BlazeditorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Blazeditor")));

// Add authentication/authorization
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.MapPost("/login", async (HttpContext context, [FromForm] string username, [FromForm] string password, SimpleAuthService authService) =>
{
    if (authService.ValidateUser(username, password))
    {
        var principal = authService.CreatePrincipal(username);
        await context.SignInAsync("Cookies", principal);
        // Redirect to home or returnUrl
        var returnUrl = context.Request.Query["returnUrl"].FirstOrDefault() ?? "/";
        context.Response.Redirect(returnUrl);
    }
    else
    {
        // Redirect back to login with error
        context.Response.Redirect("/login?error=true");
    }
});
app.UseHttpsRedirection();

app.MapStaticAssets();
app.UseAntiforgery();

// Add authentication/authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
