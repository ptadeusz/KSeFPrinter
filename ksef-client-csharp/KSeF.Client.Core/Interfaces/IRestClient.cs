using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Interfaces
{
    public interface IRestClient
    {
        /// <summary>
        /// Wysyła żądanie HTTP i zwraca odpowiedź w postaci obiektu typu TResponse.
        /// </summary>
        /// <typeparam name="TResponse">Typ obiektu odpowiedzi.</typeparam>
        /// <typeparam name="TRequest">Typ obiektu żądania.</typeparam>
        /// <param name="method">Metoda HTTP (np. GET, POST).</param>
        /// <param name="url">Adres URL żądania.</param>
        /// <param name="requestBody">Treść żądania (opcjonalne).</param>
        /// <param name="token">Token uwierzytelniający accessToken/refreshToken (opcjonalne).</param>
        /// <param name="contentType">Typ treści żądania (domyślnie "application/json").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="additionalHeaders">Dodatkowe nagłówki HTTP (opcjonalne).</param>
        /// <returns>Obiekt typu TResponse</returns>
        Task<TResponse> SendAsync<TResponse, TRequest>(HttpMethod method, string url, TRequest requestBody = default, string token = null, string contentType = "application/json", CancellationToken cancellationToken = default, Dictionary<string, string> additionalHeaders = null);

        /// <summary>
        /// Wysyła żądanie HTTP bez oczekiwania na odpowiedź w postaci obiektu.
        /// </summary>
        /// <typeparam name="TRequest">Typ obiektu żądania.</typeparam>
        /// <param name="method">Metoda HTTP (np. GET, POST).</param>
        /// <param name="url">Adres URL żądania.</param>
        /// <param name="requestBody">Treść żądania (opcjonalne).</param>
        /// <param name="token">Token uwierzytelniający accessToken/refreshToken (opcjonalne).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="cancellationToken">Token anulowania operacji (opcjonalne).</param>
        Task SendAsync<TRequest>(HttpMethod method, string url, TRequest requestBody = default, string token = null, string contentType = "application/json", CancellationToken cancellationToken = default);

        /// <summary>
        /// Wysyła żądanie HTTP bez treści żądania i bez oczekiwania na odpowiedź w postaci obiektu.
        /// </summary>
        /// <param name="method">Metoda HTTP (np. GET, POST).</param>
        /// <param name="url">Adres URL żądania.</param>
        /// <param name="token">Token uwierzytelniający accessToken/refreshToken (opcjonalne).</param>
        /// <param name="contentType">Typ treści żądania (domyślnie "application/json").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SendAsync(HttpMethod method, string url, string token = null, string contentType = "application/json", CancellationToken cancellationToken = default);
    }
}