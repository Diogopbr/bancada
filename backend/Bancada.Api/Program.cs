using Npgsql;
using Bancada.Api;
using Bancada.Api.Features.Access;
using Bancada.Api.Infrastructure;
using Bancada.Api.Features.Orders;
using Bancada.Api.Features.Quotes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions.TryAdd("code", ApiError.Code(context.ProblemDetails.Status ?? 500)));
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<BancadaDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Bancada")));
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection().SetApplicationName("Bancada").PersistKeysToDbContext<BancadaDb>();
builder.Services.Configure<ForwardedHeadersOptions>(options => ProductionHosting.ConfigureForwarding(options, builder.Configuration));
builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);
builder.Services.AddScoped<DemoResetService>();
builder.Services.AddSingleton<DemoCleanupService>();
builder.Services.AddHostedService(services => services.GetRequiredService<DemoCleanupService>());
builder.Services.AddScoped<OrderStore>();
builder.Services.AddScoped<CustomerAccessService>();
builder.Services.AddScoped<CustomerDecisionService>();
builder.Services.AddIdentity<StaffUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
}).AddEntityFrameworkStores<BancadaDb>().AddDefaultTokenProviders();
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.OnRefreshingPrincipal = context =>
    {
        var session = context.CurrentPrincipal?.FindFirst("staffSessionId");
        if (session is not null && context.NewPrincipal?.Identity is System.Security.Claims.ClaimsIdentity identity)
            identity.AddClaim(session);
        var workspace = context.CurrentPrincipal?.FindFirst("workspaceId");
        if (workspace is not null && context.NewPrincipal?.Identity is System.Security.Claims.ClaimsIdentity refreshed)
            refreshed.AddClaim(workspace);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthentication().AddCookie(CustomerAccessService.Scheme, options =>
{
    options.Cookie.Name = "bancada.customer";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = false;
});
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "bancada.staff";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
    options.Events.OnSigningIn = StaffSessionEvents.SignedIn;
    options.Events.OnValidatePrincipal = StaffSessionEvents.Validate;
    options.Events.OnSigningOut = StaffSessionEvents.SignedOut;
    options.Events.OnRedirectToLogin = context => ApiError.Result(401).ExecuteAsync(context.HttpContext);
    options.Events.OnRedirectToAccessDenied = context => ApiError.Result(403).ExecuteAsync(context.HttpContext);
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("staff", policy => policy.RequireRole("receptionist", "technician"));
    options.AddPolicy("receptionist", policy => policy.RequireRole("receptionist"));
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "bancada.csrf";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = (context, _) => new ValueTask(ApiError.Result(429).ExecuteAsync(context.HttpContext));
    foreach (var name in new[] { "login", "customer", "demo" })
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
if (args.Contains("--bootstrap"))
{
    await Bootstrap.RunAsync(app.Services, args.Contains("--seed"));
    return;
}
app.UseExceptionHandler();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.Use(async (http, next) =>
{
    try { await next(); }
    catch (ApiError error) { await ApiError.Result(error.Status, error.Detail).ExecuteAsync(http); }
    catch (DbUpdateConcurrencyException) { await ApiError.Result(409).ExecuteAsync(http); }
});
app.UseStatusCodePages(async context =>
{
    var status = context.HttpContext.Response.StatusCode;
    if (status is 400 or 401 or 403 or 404 or 405 or 429)
        await ApiError.Result(status == 405 ? 400 : status).ExecuteAsync(context.HttpContext);
});
app.Use(async (http, next) =>
{
    if (http.Request.Path.StartsWithSegments("/api"))
        await http.RequestServices.GetRequiredService<DemoCleanupService>().CleanupIfDueAsync(http.RequestAborted);
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.Use(async (http, next) =>
{
    if (http.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(http.Request.Method)
        && !HttpMethods.IsHead(http.Request.Method) && !HttpMethods.IsOptions(http.Request.Method))
    {
        try { await http.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(http); }
        catch (AntiforgeryValidationException)
        {
            await ApiError.Result(400, "Token CSRF invalido.").ExecuteAsync(http);
            return;
        }
    }
    await next();
});
app.MapStaff();
app.MapOrders();
app.MapQuotes();
app.MapCustomer();
app.MapDemo();
app.MapSpa();
app.MapOpenApi();
app.MapGet("/health/ready", async (IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var connectionString = configuration.GetConnectionString("Bancada");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    try
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);
        return Results.Ok(new { status = "ready" });
    }
    catch (NpgsqlException)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).WithName("Readiness");

app.Run();

public partial class Program { }
