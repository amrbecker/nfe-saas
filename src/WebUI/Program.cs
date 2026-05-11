using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using NfeSaas.WebUI;
using NfeSaas.WebUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API HttpClient
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5001")
});

// MudBlazor
builder.Services.AddMudServices();

// LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Auth
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();

// App Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotaFiscalService, NotaFiscalService>();
builder.Services.AddScoped<IEmpresaService, EmpresaService>();
builder.Services.AddScoped<IEscritorioService, EscritorioService>();
builder.Services.AddScoped<IConfiguracaoEmpresaService, ConfiguracaoEmpresaService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IEventoFiscalService, EventoFiscalService>();
builder.Services.AddSingleton<IViaCepService, ViaCepService>();
builder.Services.AddSingleton<IReceitaApiService, ReceitaApiService>();
builder.Services.AddScoped<INcmService, NcmService>();
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
