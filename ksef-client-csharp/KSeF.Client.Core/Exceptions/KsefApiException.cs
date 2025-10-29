using System;
using System.Net;

namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Reprezentuje ustrukturyzowany wyjątek API zawierający szczegóły błędu zwrócone przez interfejs API.
    /// </summary>
    public class KsefApiException : Exception
    {
        /// <summary>
        /// Kod stanu HTTP odpowiedzi.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Opcjonalny kod usługi z błędu API.
        /// </summary>
        public string ServiceCode { get; }

        /// <summary>
        /// Opcjonalna odpowiedź błędu z API.
        /// </summary>
        public ApiErrorResponse Error { get; }

        /// <summary>
        /// Inicjalizuje nową instancję klasy <see cref="KsefApiException"/>.
        /// </summary>
        /// <param name="message">Szczegółowy komunikat wyjątku.</param>
        /// <param name="statusCode">Kod stanu HTTP.</param>
        /// <param name="serviceCode">Opcjonalny kod usługi z API.</param>
        /// <param name="error">Szczegóły błędu zwrócone przez API (opcjonalnie).</param>
        public KsefApiException(string message, HttpStatusCode statusCode, string serviceCode = null, ApiErrorResponse error = null)
            : base(message)
        {
            StatusCode = statusCode;
            ServiceCode = serviceCode;
            Error = error;
        }
    }
}