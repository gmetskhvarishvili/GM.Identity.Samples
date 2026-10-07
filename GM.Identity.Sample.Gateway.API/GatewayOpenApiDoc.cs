using System.Text.Json.Nodes;

using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
namespace GM.Identity.Sample.Gateway.API;

/// <summary>
/// Serves the gateway's OpenAPI document: it fetches the backend Identity API's document, stamps a
/// gateway-specific title/description, and (unless <c>IncludeAll</c>) keeps only the operations this
/// gateway actually exposes â€” so Swagger shows exactly what the gateway proxies, not the whole API.
/// Configured from the "Documentation" section; add an <c>Include</c> rule to expose more later.
/// </summary>
public static class GatewayOpenApiDoc
{
    public sealed class IncludeRule
    {
        public string? PathPrefix { get; set; }
        public string? Path { get; set; }
        public string[]? Methods { get; set; }
    }

    public sealed class RewriteRule
    {
        public string? Path { get; set; }       // gateway path to expose, e.g. "/me"
        public string? FromPath { get; set; }   // version-less backend path to clone, e.g. "/api/users/{id}"
        public string? Method { get; set; }     // operation to clone, e.g. "get"
        public string? Tag { get; set; }        // optional group override
        public string? Summary { get; set; }    // optional summary override
    }

    public static void MapGatewayOpenApiDoc(this WebApplication app, string route = "/api-docs/identity/v1.json")
    {
        app.MapGet(route, async (IHttpClientFactory factory, IConfiguration configuration) =>
        {
            var section = configuration.GetSection("Documentation");

            var client = factory.CreateClient("identity-docs");
            var json = await client.GetStringAsync(section["Backend"]!);
            var root = JsonNode.Parse(json)!.AsObject();

            var info = root["info"]?.AsObject() ?? new JsonObject();
            if (section["Title"] is { Length: > 0 } title) info["title"] = title;
            if (section["Description"] is { Length: > 0 } description) info["description"] = description;
            root["info"] = info;

            var originalPaths = root["paths"] as JsonObject;
            var resultPaths = originalPaths is not null && !section.GetValue<bool>("IncludeAll")
                ? FilterPaths(originalPaths, section.GetSection("Include").Get<List<IncludeRule>>() ?? [])
                : originalPaths ?? new JsonObject();

            // Expose token-derived gateway routes (e.g. /me) by cloning the backend operation they map to.
            foreach (var rewrite in section.GetSection("Rewrites").Get<List<RewriteRule>>() ?? [])
                ApplyRewrite(originalPaths, resultPaths, rewrite);

            root["paths"] = resultPaths;

            if (!section.GetValue<bool>("IncludeAll"))
                PruneTags(root, resultPaths);

            return Results.Text(root.ToJsonString(), "application/json");
        });
    }

    private static JsonObject FilterPaths(JsonObject paths, List<IncludeRule> rules)
    {
        var kept = new JsonObject();

        foreach (var (path, operations) in paths)
        {
            if (operations is null) continue;

            // Match against the version-less path (/api/v1.0/accounts -> /api/accounts) so rules survive
            // a change in the version segment format.
            var canonical = StripVersion(path);
            var rule = rules.FirstOrDefault(r => Matches(r, canonical));
            if (rule is null) continue;

            if (rule.Methods is { Length: > 0 })
            {
                var keptOperations = new JsonObject();
                foreach (var (method, operation) in operations.AsObject())
                    if (rule.Methods.Contains(method, StringComparer.OrdinalIgnoreCase))
                        keptOperations[method] = operation!.DeepClone();

                if (keptOperations.Count > 0) kept[path] = keptOperations;
            }
            else
            {
                kept[path] = operations.DeepClone();
            }
        }

        return kept;
    }

    // Clones a backend operation under a new gateway path (stripping path parameters like {id}) so a
    // token-derived route such as /me shows in Swagger with the backend's real request/response schema.
    private static void ApplyRewrite(JsonObject? originalPaths, JsonObject resultPaths, RewriteRule rule)
    {
        if (originalPaths is null || rule.Path is null || rule.FromPath is null || rule.Method is null) return;

        var source = FindOperation(originalPaths, rule.FromPath, rule.Method);
        if (source is null) return;

        var operation = source.DeepClone()!.AsObject();

        if (operation["parameters"] is JsonArray parameters)
        {
            var kept = new JsonArray();
            foreach (var parameter in parameters)
                if (parameter?["in"]?.GetValue<string>() is not "path")
                    kept.Add(parameter!.DeepClone());
            operation["parameters"] = kept;
        }

        if (rule.Tag is not null) operation["tags"] = new JsonArray(rule.Tag);
        if (rule.Summary is not null) operation["summary"] = rule.Summary;

        resultPaths[rule.Path] = new JsonObject { [rule.Method.ToLowerInvariant()] = operation };
    }

    private static JsonNode? FindOperation(JsonObject paths, string canonicalFromPath, string method)
    {
        foreach (var (path, operations) in paths)
        {
            if (operations is not JsonObject ops) continue;
            if (!string.Equals(StripVersion(path), canonicalFromPath, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var (operationMethod, operation) in ops)
                if (string.Equals(operationMethod, method, StringComparison.OrdinalIgnoreCase))
                    return operation;
        }

        return null;
    }

    // Drops root-level tag groups that no kept operation references, so Swagger shows no empty groups.
    private static void PruneTags(JsonObject root, JsonObject keptPaths)
    {
        if (root["tags"] is not JsonArray tags) return;

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, operations) in keptPaths)
        {
            if (operations is not JsonObject ops) continue;
            foreach (var (_, operation) in ops)
                if (operation?["tags"] is JsonArray operationTags)
                    foreach (var tag in operationTags)
                        if (tag is not null) used.Add(tag.GetValue<string>());
        }

        var kept = new JsonArray();
        foreach (var tag in tags)
            if (tag?["name"]?.GetValue<string>() is { } name && used.Contains(name))
                kept.Add(tag.DeepClone());

        root["tags"] = kept;
    }

    private static bool Matches(IncludeRule rule, string canonicalPath) =>
        (rule.PathPrefix is not null && canonicalPath.StartsWith(rule.PathPrefix, StringComparison.OrdinalIgnoreCase)) ||
        (rule.Path is not null && string.Equals(canonicalPath, rule.Path, StringComparison.OrdinalIgnoreCase));

    private static string StripVersion(string path)
    {
        var segments = path.TrimStart('/').Split('/');
        return segments is [var api, _, ..] && api.Equals("api", StringComparison.OrdinalIgnoreCase)
            ? "/" + string.Join('/', segments.Where((_, i) => i != 1))
            : path;
    }
}
