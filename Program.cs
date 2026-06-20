using Microsoft.EntityFrameworkCore;
using rytmix_api.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Services (dependency injection container)
// ---------------------------------------------------------------------------

// MVC controllers. The actual endpoints (Tracks, Playlists, Favorites, Auth)
// live under Controllers/ and are added later (none yet).
builder.Services.AddControllers();

// OpenAPI document — exposed in Development only (see pipeline below). Handy
// for the team to explore the API while building.
builder.Services.AddOpenApi();

// EF Core + PostgreSQL (Npgsql). The connection string is read from
// configuration key "ConnectionStrings:DefaultConnection". It is NEVER
// hardcoded or committed:
//   - locally: put it in appsettings.Development.json (gitignored) or user-secrets
//   - on Render: set the env var ConnectionStrings__DefaultConnection
// Registering the context here is safe even with no connection string yet —
// nothing resolves it yet (the /health endpoint never touches the DB).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// CORS — the frontend (Cloudflare Pages/localhost) and this API are different origins,
// so the browser requires the API to opt the frontend origin in. Origins are
// read from config ("Cors:AllowedOrigins"), so they are configurable per
// environment (e.g. localhost in dev, the Cloudflare Pages URL in prod, and later the
// Tauri desktop origin) without code changes.
const string FrontendCorsPolicy = "FrontendCors";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// HTTP request pipeline
// ---------------------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Note: no UseHttpsRedirection here. In production, Render terminates TLS at
// its edge and forwards plain HTTP to the container, so an in-container HTTPS
// redirect would be wrong (and only adds noise). Local dev can still use the
// https launch profile directly.

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

// Lightweight liveness/keep-alive probe. Returns 200 and deliberately does NOT
// touch the database — the cron-job.org keep-alive pings this every 5 minutes,
// so it must stay cheap.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

app.Run();
