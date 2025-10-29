using System.Collections.Generic;

namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Reprezentuje pojedynczy szczegół wyjątku w odpowiedzi błędu API.
    /// </summary>
    public class ApiExceptionDetail
    {
        /// <summary>
        /// Numeryczny kod reprezentujący typ wyjątku.
        /// </summary>
        public int ExceptionCode { get; set; }

        /// <summary>
        /// Czytelny dla człowieka opis wyjątku.
        /// </summary>
        public string ExceptionDescription { get; set; }

        /// <summary>
        /// Opcjonalna lista dodatkowych komunikatów kontekstowych.
        /// </summary>
        public List<string> Details { get; set; }
    }
}