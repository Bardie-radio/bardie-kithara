using System.Security.Claims;
using System.Text.Json.Serialization;
using Bardie.Auth.Orchestrator;
using Bardie.Auth.Orchestrator.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Kithara.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapGet("/discovery", async (AuthModuleOrchestrator orch, CancellationToken ct) =>
        {
            var providers = await orch.GetProvidersAsync(ct).ConfigureAwait(false);
            return Results.Ok(new
            {
                providers = providers.Select(p => new
                {
                    id = p.Id,
                    display_name = p.DisplayName,
                    module = p.ModuleSlug,
                    ui_mode = p.UiMode,
                    form_fields = p.FormFields.Select(f => new
                    {
                        name = f.Name,
                        label = f.Label,
                        input_type = f.InputType,
                        required = f.Required,
                    }),
                    authorize_url = p.AuthorizeUrl,
                }),
            });
        });

        group.MapPost("/authenticate", async (
            [FromBody] AuthenticateRequestBody body,
            AuthModuleOrchestrator orch,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.ProviderId))
            {
                return Results.BadRequest(new { error = "provider_id is required." });
            }

            var payload = body.Payload ?? new Dictionary<string, string>();
            var result = await orch.AuthenticateAsync(body.ProviderId, payload, ct).ConfigureAwait(false);
            if (!result.Allowed)
            {
                return Results.Json(
                    new { error = result.FailureReason ?? "unauthorized" },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(new
            {
                access_token = result.AccessToken,
                refresh_token = result.RefreshToken,
                token_type = result.TokenType,
                expires_in = result.ExpiresIn,
                must_rotate_credentials = result.MustRotateCredentials,
                user_id = result.UserId,
                external_subject = result.ExternalSubject,
            });
        });

        group.MapPost("/refresh", async (
            [FromBody] RefreshRequestBody body,
            AuthModuleOrchestrator orch,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.ProviderId) || string.IsNullOrWhiteSpace(body.RefreshToken))
            {
                return Results.BadRequest(new { error = "provider_id and refresh_token are required." });
            }

            var result = await orch.RefreshAsync(body.ProviderId, body.RefreshToken, ct).ConfigureAwait(false);
            if (!result.Allowed)
            {
                return Results.Json(
                    new { error = result.FailureReason ?? "unauthorized" },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(new
            {
                access_token = result.AccessToken,
                refresh_token = result.RefreshToken,
                token_type = result.TokenType,
                expires_in = result.ExpiresIn,
            });
        });

        group.MapGet("/me", async (
            ClaimsPrincipal user,
            IAuthPersistence persistence,
            AuthModuleOrchestrator orch,
            CancellationToken ct) =>
        {
            var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            var provider = user.FindFirstValue("bardie_provider")
                ?? orch.ListRegisteredModules().FirstOrDefault()?.Slug;
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(provider))
            {
                return Results.Unauthorized();
            }

            var record = await persistence.FindUserByBindingSubjectAsync(provider, subject, ct)
                .ConfigureAwait(false);
            if (record is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                user_id = record.UserId,
                kind = record.Kind,
                status = record.Status,
                external_subject = subject,
                provider,
                must_rotate_credentials = record.MustRotateCredentials,
            });
        }).RequireAuthorization();

        return endpoints;
    }

    public sealed class AuthenticateRequestBody
    {
        [JsonPropertyName("provider_id")]
        public string ProviderId { get; set; } = string.Empty;

        [JsonPropertyName("payload")]
        public Dictionary<string, string>? Payload { get; set; }
    }

    public sealed class RefreshRequestBody
    {
        [JsonPropertyName("provider_id")]
        public string ProviderId { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
