using GymApp.Api.Extensions;
using GymApp.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

// --- Service registration (see Extensions/*) ---
builder.AddCommonInfrastructure();   // Serilog, clock, JWT service, outbox, tenancy, Redis, Quartz, MediatR
builder.AddJwtAuthentication();      // JWT bearer + secret fail-fast + SignalR token handoff
builder.AddAuthorizationPolicies();  // claim/role-based policies
builder.AddApplicationModules();     // assembly force-load + 9 feature modules
builder.AddApiServices();            // exception handler, JSON, Swagger, SignalR, health, CORS

var app = builder.Build();

// --- Startup (see Startup/*) ---
await app.RunHardTenancyBootstrapAsync();  // Hard-mode version gate + fresh-DB self-bootstrap
await app.InitializeDatabaseAsync();        // EF migrations + reference-data/demo seeders

// --- HTTP pipeline + endpoint mapping (see Extensions/PipelineExtensions) ---
app.UseApiPipeline();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
