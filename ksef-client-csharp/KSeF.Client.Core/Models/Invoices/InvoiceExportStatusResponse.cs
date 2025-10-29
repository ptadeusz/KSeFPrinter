using System;

namespace KSeF.Client.Core.Models.Invoices
{
    public class InvoiceExportStatusResponse
    {
        /// <summary>
        /// Aktualny status operacji (np. IN_PROGRESS, READY, ERROR).
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// Data zakończenia przetwarzania żądania.
        /// </summary>
        public DateTimeOffset? CompletedDate { get; set; }

        /// <summary>
        /// Paczka faktur (InvoicePackage) – obecna tylko jeśli status = READY.
        /// </summary>
        public InvoicePackage Package { get; set; }
    }
}
