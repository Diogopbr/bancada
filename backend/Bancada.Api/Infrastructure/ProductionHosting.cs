using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Bancada.Api.Infrastructure;

public static class ProductionHosting
{
    public static void ConfigureForwarding(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.RequireHeaderSymmetry = true;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        if (configuration.GetValue<bool>("Hosting:VercelIngress"))
        {
            if (configuration["VERCEL"] != "1")
                throw new InvalidOperationException("Vercel ingress requires the Vercel deployment environment.");
            // Only enable inside the gateway-only Vercel container runtime. Vercel overwrites this IP header.
            // A self-hosted public Kestrel server must use explicit trusted proxies below instead.
            options.ForwardedForHeaderName = "x-vercel-forwarded-for";
            return;
        }
        // Trust only the immediate proxy controlled by the host, never arbitrary public forwarding headers.
        foreach (var value in configuration.GetSection("Hosting:TrustedProxies").Get<string[]>() ?? [])
            options.KnownProxies.Add(IPAddress.Parse(value));
        foreach (var value in configuration.GetSection("Hosting:TrustedNetworks").Get<string[]>() ?? [])
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(value));
        if (options.KnownProxies.Count == 0 && options.KnownIPNetworks.Count == 0)
            options.ForwardedHeaders = ForwardedHeaders.None;
    }

    public static void MapSpa(this WebApplication app)
    {
        app.MapFallback(async http =>
        {
            if (http.Request.Path.StartsWithSegments("/api") || http.Request.Path.StartsWithSegments("/health")
                || Path.HasExtension(http.Request.Path.Value)
                || !(HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method)))
            {
                http.Response.StatusCode = 404;
                return;
            }
            var file = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
            if (!file.Exists)
            {
                http.Response.StatusCode = 404;
                return;
            }
            http.Response.ContentType = "text/html; charset=utf-8";
            http.Response.Headers.CacheControl = "no-cache";
            if (!HttpMethods.IsHead(http.Request.Method)) await http.Response.SendFileAsync(file);
        });
    }
}
