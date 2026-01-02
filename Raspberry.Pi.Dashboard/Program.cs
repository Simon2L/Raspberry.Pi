using MudBlazor.Services;
using Raspberry.Pi.Dashboard;
using Raspberry.Pi.Dashboard.Components;
using Raspberry.Pi.Dashboard.Events.Publishers;
using Raspberry.Pi.Dashboard.Handlers;
using Raspberry.Pi.Dashboard.Integration;
using Raspberry.Pi.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5000");
// builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddHttpClient<ISLApiService, SLApiService>(client =>
{
    client.BaseAddress = new Uri("https://transport.integration.sl.se/v1/");
});

builder.Services.AddHttpClient<GoveeClient>(client =>
{
    client.BaseAddress = new Uri("https://openapi.api.govee.com/router/api/v1/");
    client.DefaultRequestHeaders.Add("Govee-API-Key", "560fbc36-952c-42f7-925b-a7151394f3c5");
});

builder.Services.AddHostedService<ProximitySensorReaderBackgroundService>();
builder.Services.AddHostedService<ProximityEventHandler>();
builder.Services.AddHostedService<ProximityUIHandler>();

builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IApplicationStateService, ApplicationStateService>();
builder.Services.AddSingleton<IProximityEventPublisher, ProximityEventPublisher>();

//sites/9296/departures

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();

/*
using (var scope = app.Services.CreateScope())
{
    var goveeClient = scope.ServiceProvider.GetService<GoveeClient>();

    try
    {
        if (goveeClient != null)
        {
            Console.WriteLine("Setting whole strip to 1");
            await goveeClient.SetBrightnessAsync(100);
            // await goveeClient.SetColorRgbAsync(new RGB(255, 0, 0));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("error while setting the brightness to 1 " + ex.Message);
    }
}
*/

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
