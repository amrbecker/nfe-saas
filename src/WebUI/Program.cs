using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using NfeSaas.WebUI;
using NfeSaas.WebUI.Services;
using NfeSaas.WebUI.Services.Assistente;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Sentry:Dsn vazio (default) = SDK inativo. Configure em wwwroot/appsettings.Production.json
// (ou via build da Cloudflare Pages) para ativar captura de erros client-side.
builder.UseSentry(o =>
{
    o.Dsn = builder.Configuration["Sentry:Dsn"];
    o.TracesSampleRate = 0.1;
});

// API HttpClient
// ErrosApiHandler alimenta o contexto da Ori com os últimos erros HTTP da API (CAPTURA_CONTEXTO.md).
builder.Services.AddScoped(sp => new HttpClient(new ErrosApiHandler(sp.GetRequiredService<IContextoAssistente>())
{
    InnerHandler = new HttpClientHandler()
})
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
builder.Services.AddScoped<ICnaeService, CnaeService>();
builder.Services.AddScoped<IPersonalizacaoService, PersonalizacaoService>();
builder.Services.AddScoped<ApiClient>();

// Assistente Ori
builder.Services.AddAssistenteUi();

await builder.Build().RunAsync();
