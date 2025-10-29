using System;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Invoices
{
    public class InvoicePackage
    {
        /// <summary>
        ///  Łączna liczba faktur w paczce. Maksymalna liczba faktur w paczce to 10 000.
        /// </summary>
        public long InvoiceCount { get; set; }

        /// <summary>
        /// Rozmiar paczki w bajtach. Maksymalny rozmiar paczki to 1 GiB (1 073 741 824 bajtów).
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Lista dostępnych części paczki do pobrania. Każda część jest zaszyfrowana algorytmem AES-256-CBC z dopełnieniem PKCS#7.
        /// </summary>
        public List<InvoicePackagePart> Parts { get; set; }

        /// <summary>
        /// Określa, czy wynik eksportu został ucięty z powodu przekroczenia limitu liczby faktur lub wielkości paczki.
        /// </summary>
        public bool IsTruncated { get; set; }

        /// <summary>
        /// Data wystawienia ostatniej faktury ujętej w paczce. Występuje wyłącznie gdy paczka została ucięta i eksport był filtrowany po typie daty Issue.
        /// </summary>
        public DateTimeOffset? LastIssueDate { get; set; }

        /// <summary>
        /// Data przyjęcia ostatniej faktury ujętej w paczce. Występuje wyłącznie gdy paczka została ucięta i eksport był filtrowany po typie daty Invoicing.
        /// </summary>
        public DateTimeOffset? LastInvoicingDate { get; set; }

        /// <summary>
        /// Data trwałego zapisu ostatniej faktury ujętej w paczce. Występuje wyłącznie gdy paczka została ucięta i eksport był filtrowany po typie daty PermanentStorage.
        /// </summary>
        public DateTimeOffset? LastPermanentStorageDate { get; set; }
    }

}
