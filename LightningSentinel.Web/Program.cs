using LightningSentinel.Web;
using LightningSentinel.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient("api", client =>
{
    // "apiservice" must match the name in your AppHost Program.cs
    client.BaseAddress = new Uri("https+http://apiservice");
})
.AddServiceDiscovery();

builder.Services.AddHttpClient<ProbeHttpClient>(client =>
{
    client.BaseAddress = new Uri("https+http://apiservice"); // Use service discovery scheme
})
.AddServiceDiscovery();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
