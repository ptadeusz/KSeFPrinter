using System;
using System.Collections.Generic;

namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Zawiera szczegółowe metadane wyjątku, w tym kod, opis i znacznik czasu.
    /// </summary>
    public class ApiExceptionContent
    {
        /// <summary>
        /// Lista szczegółów wyjątków opisujących poszczególne problemy.
        /// </summary>
        public List<ApiExceptionDetail> ExceptionDetailList { get; set; }

        /// <summary>
        /// Unikalny kod reprezentujący instancję usługi, która wygenerowała błąd.
        /// </summary>
        public string ServiceCode { get; set; }

        /// <summary>
        /// Znacznik czasu wystąpienia wyjątku.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Nazwa usługi, w której wystąpił błąd.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Numer referencyjny służący do korelacji żądania i błędu.
        /// </summary>
        public string ReferenceNumber { get; set; }

        /// <summary>
        /// Dodatkowy kontekst usługi/
        /// </summary>
        public string ServiceCtx { get; set; }
    }
}