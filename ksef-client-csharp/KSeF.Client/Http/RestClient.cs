using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KSeF.Client.Http;

/// <summary>
/// A generic REST client that supports GET, POST, and DELETE requests with optional authorization,
/// content serialization/deserialization, and structured error handling.
/// </summary>
public class RestClient : IRestClient
{
    private readonly HttpClient httpClient;

    public const string DefaultContentType = "application/json";
    public const string XmlContentType = "application/xml";

    public RestClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <summary>
    /// Sends an HTTP request and returns the deserialized response.
    /// </summary>
    public async Task<TResponse> SendAsync<TResponse, TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string bearerToken = null,
        string contentType = DefaultContentType,
        CancellationToken cancellationToken = default,
        Dictionary<string, string> additionalHeaders = default)
    {

        using var request = new HttpRequestMessage(method, url);

        if (requestBody != null && method != HttpMethod.Get)
        {
            var requestContent = contentType == DefaultContentType
                ? JsonUtil.Serialize(requestBody)
                : requestBody.ToString();

            request.Content = new StringContent(requestContent!, Encoding.UTF8, contentType);
        }

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        var ct = cancellationToken;

        using var response = await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        string responseText = response.Content != null
            ? await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false)
            : null;

        HandleInvalidStatusCode(response, responseText);

        if (string.IsNullOrEmpty(responseText))
            return default;

        if (typeof(TResponse) == typeof(string))
            return (TResponse)(object)responseText;

        return JsonUtil.Deserialize<TResponse>(responseText);
    }

    private static void HandleInvalidStatusCode(HttpResponseMessage response, string responseText)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KsefApiException("Not found", response.StatusCode);
        }

        try
        {
            if (string.IsNullOrEmpty(responseText))
            {
                throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", response.StatusCode);
            }

            var error = JsonUtil.Deserialize<ApiErrorResponse>(responseText);
            string fullMessage = string.Empty;

            if (error?.Exception?.ExceptionDetailList?.Any() == true)
            {
                var errorMessages = error
                    .Exception
                    .ExceptionDetailList
                    .Select(detail =>
                        $"{detail.ExceptionCode}: {detail.ExceptionDescription} - {string.Join("; ", detail.Details ?? new List<string>())}");
                fullMessage = string.Join(" | ", errorMessages);
            }

            throw new KsefApiException(fullMessage, response.StatusCode, error.Exception.ServiceCode, error);
        }
        catch (JsonException e)
        {
            throw new KsefApiException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}, AdditionalInfo: {e.Message}", response.StatusCode);
        }
    }

    /// <summary>
    /// Sends an HTTP request.
    /// </summary>
    public async Task SendAsync<TRequest>(
        HttpMethod method,
        string url,
        TRequest requestBody = default,
        string bearerToken = null,
        string contentType = DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        await SendAsync<object, TRequest>(method, url, requestBody, bearerToken, contentType, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an HTTP request without a body.
    /// </summary>
    public async Task SendAsync(
        HttpMethod method,
        string url,
        string bearerToken = null,
        string contentType = DefaultContentType,
        CancellationToken cancellationToken = default)
    {
        await SendAsync<object>(method, url, null, bearerToken, contentType, cancellationToken)
            .ConfigureAwait(false);
    }
}
