using System.ComponentModel.DataAnnotations;
using Bancada.Api;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Features.Access;

public static class StaffEndpoints
{
    public static void MapStaff(this WebApplication app)
    {
        app.MapGet("/api/csrf", (HttpContext http, IAntiforgery antiforgery) =>
            Results.Ok(new { token = antiforgery.GetAndStoreTokens(http).RequestToken }));
        app.MapGet("/api/session", async (ClaimsPrincipal principal, UserManager<StaffUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            return user is null ? ApiError.Result(401) : Results.Ok(await Session(user, users));
        }).RequireAuthorization("staff");
        app.MapPost("/api/login", async (LoginRequest request, UserManager<StaffUser> users, SignInManager<StaffUser> signIn,
            IConfiguration configuration) =>
        {
            if (configuration.GetValue<bool>("BANCADA_DEMO_MODE")) return ApiError.Result(401);
            if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email)
                || string.IsNullOrEmpty(request.Password) || request.Password.Length > 1024)
                return ApiError.Result(400);
            var user = await users.FindByEmailAsync(request.Email);
            if (user is null || user.WorkspaceId is not null) return ApiError.Result(401);
            var result = await signIn.PasswordSignInAsync(user, request.Password, false, true);
            return result.Succeeded ? Results.Ok(await Session(user, users)) : ApiError.Result(401);
        }).RequireRateLimiting("login");
        app.MapPost("/api/logout", async (SignInManager<StaffUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization("staff");
        app.MapGet("/api/technicians", async (Bancada.Api.Infrastructure.BancadaDb db) =>
            Results.Ok(await db.Users.Where(u => u.WorkspaceId == db.CurrentWorkspaceId
                && db.UserRoles.Any(ur => ur.UserId == u.Id
                    && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "TECHNICIAN")))
                .OrderBy(u => u.DisplayName).Select(u => new { u.Id, u.DisplayName }).ToListAsync()))
            .RequireAuthorization("staff");
    }

    internal static async Task<object> Session(StaffUser user, UserManager<StaffUser> users) =>
        new { user.Id, user.DisplayName, role = await users.IsInRoleAsync(user, "receptionist") ? "receptionist" : "technician" };
}

public sealed record LoginRequest(string Email, string Password);
