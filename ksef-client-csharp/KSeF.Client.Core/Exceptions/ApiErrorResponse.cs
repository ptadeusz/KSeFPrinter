namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Reprezentuje ustrukturyzowaną odpowiedź błędu zwracaną przez interfejs API.
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// Zawiera główną treść wyjątku wraz ze szczegółami.
        /// </summary>
        public ApiExceptionContent Exception { get; set; }
    }
}