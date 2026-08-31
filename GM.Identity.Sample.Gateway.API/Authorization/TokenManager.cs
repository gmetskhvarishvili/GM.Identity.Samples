using System.Text;
using System.Text.Json;

namespace GM.Identity.Sample.Gateway.API.Authorization;

public class TokenManager(IHttpContextAccessor httpContextAccessor) : ITokenManager
{
    private readonly Dictionary<string, string> _dictionary = new();
    private string? _json;
    
    public string? GetClaim(string key)
    {
        if (_dictionary.TryGetValue(key, out var dicValue))
        {
            return dicValue;
        }

        if (string.IsNullOrWhiteSpace(_json))
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null) return null;

            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;

            var token = authHeader["Bearer ".Length..].Trim();
            var parts = token.Split('.');
            if (parts.Length < 2) return null;

            try
            {
                var payload = PadBase64(parts[1]);
                _json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            }
            catch (Exception)
            {
                return null;
            }
        }

        try
        {
            using var doc = JsonDocument.Parse(_json);
            var root = doc.RootElement;

            if (!root.TryGetProperty(key, out var value)) return null;
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    dicValue = value.GetString();
                    _dictionary.Add(key, dicValue!);
                    return dicValue;
                case JsonValueKind.Array:
                {
                    var strings = value.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString());
                    dicValue = string.Join(",", strings);
                    _dictionary.Add(key, dicValue);
                    return dicValue;
                }
                case JsonValueKind.Undefined:
                    break;
                case JsonValueKind.Object:
                    break;
                case JsonValueKind.Number:
                    break;
                case JsonValueKind.True:
                    break;
                case JsonValueKind.False:
                    break;
                case JsonValueKind.Null:
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported JSON value kind '{value.ValueKind}' for claim '{key}'.");
            }

            return null;
        }
        catch
        {
            // Optionally log the error
            return null;
        }
    }

    private static string PadBase64(string input)
    {
        var padding = 4 - input.Length % 4;
        if (padding is > 0 and < 4)
            input = input.PadRight(input.Length + padding, '=');

        return input.Replace('-', '+').Replace('_', '/');
    }
}