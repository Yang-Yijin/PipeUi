using PipeUi;
//using PipeUi.Components;
using PipeUi.Interfaces;
using PipeUi.Services;
using PipeUi.State;
using PipeUi.Views;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IVoltageService, VoltageService>();

//  MVVM-ish：State = ViewModel
builder.Services.AddScoped<VoltagePlotState>();

builder.Services.AddScoped<ModalState>();

// Python bridge: singleton so the Python process persists across page navigations
builder.Services.AddSingleton<PythonBridge>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
