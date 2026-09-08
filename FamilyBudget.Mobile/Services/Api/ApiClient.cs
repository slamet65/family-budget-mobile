using System.Net.Http.Json;
using System.Text.Json;
using FamilyBudget.Mobile.Common;
using FamilyBudget.Mobile.Services.Auth;

namespace FamilyBudget.Mobile.Services.Api;

public partial class ApiClient(HttpClient http, SessionExpiredNotifier sessionExpiredNotifier) : IApiClient
{
    private async Task<TResponse> SendAsync<TResponse>(
        HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, uri, body, ct);
        var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions.Default, ct);
        return result!;
    }

    private async Task SendAsync(HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, uri, body, ct);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(method, uri);
            if (body is not null)
            {
                // Explicit inputType (rather than the generic Create<T> overload, where T would
                // be inferred as `object` from this method's `object? body` parameter) so
                // System.Text.Json serializes the request DTO's actual declared properties
                // instead of whatever `object` reflection would otherwise produce.
                request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions.Default);
            }

            response = await http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ApiException(null, "Couldn't reach the server, check your connection and try again.");
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            var message = await TryReadErrorMessageAsync(response, ct);

            if (statusCode == 401)
            {
                sessionExpiredNotifier.Notify();
            }

            throw new ApiException(statusCode, message);
        }
    }

    // Two distinct shapes come back from the API on 4xx: hand-written checks return
    // {"error": "plain message"}, but @hono/zod-validator's own schema-parsing failures return
    // {"error": {"issues": [{"message": "...", ...}, ...], "name": "ZodError"}} -- `error` is an
    // object, not a string, there. Both are handled here via JsonDocument (rather than
    // deserializing straight into a fixed-shape record) so neither is silently swallowed into a
    // generic "Request failed" message.
    private static async Task<string> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var genericMessage = $"Request failed ({(int)response.StatusCode}).";
        try
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("code", out var codeElement) && codeElement.ValueKind == JsonValueKind.String)
            {
                var localized = codeElement.GetString() switch
                {
                    "SAVING_BALANCE_INSUFFICIENT" => "Saldo tabungan tidak mencukupi untuk perubahan ini. Sesuaikan pengeluaran tabungan terlebih dahulu.",
                    "SAVING_TRANSACTION_READ_ONLY" => "Transaksi tabungan otomatis hanya dapat diubah melalui transaksi anggaran asalnya.",
                    "CATEGORY_HAS_CHILDREN" => "Kategori yang memiliki subkategori tidak dapat dihubungkan ke tabungan.",
                    "SAVING_CATEGORY_CANNOT_BE_PARENT" => "Kategori tabungan tidak dapat memiliki subkategori.",
                    "CATCH_ALL_CANNOT_MAP_TO_SAVING" => "Kategori Lain-lain tidak dapat dihubungkan ke tabungan.",
                    "SAVING_NAME_EXISTS" => "Nama tabungan sudah digunakan.",
                    "CLOSED_PERIOD_TRANSACTION" => "Transaksi pada periode yang sudah ditutup tidak dapat diubah.",
                    _ => null,
                };
                if (localized is not null) return localized;
            }
            if (!doc.RootElement.TryGetProperty("error", out var error))
            {
                return genericMessage;
            }

            if (error.ValueKind == JsonValueKind.String)
            {
                return error.GetString() ?? genericMessage;
            }

            if (error.ValueKind == JsonValueKind.Object &&
                error.TryGetProperty("issues", out var issues) &&
                issues.ValueKind == JsonValueKind.Array)
            {
                var messages = issues.EnumerateArray()
                    .Select(issue => issue.TryGetProperty("message", out var m) ? m.GetString() : null)
                    .Where(m => !string.IsNullOrEmpty(m));
                var joined = string.Join("; ", messages);
                return string.IsNullOrEmpty(joined) ? genericMessage : joined;
            }

            return genericMessage;
        }
        catch
        {
            return genericMessage;
        }
    }
}
