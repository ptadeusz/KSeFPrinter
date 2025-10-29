using System;

namespace KSeF.Client.Core.Models.Invoices
{
    public class InvoicePackagePart
    {
        /// <summary>
        /// Numer sekwencyjny pliku części paczki.
        /// </summary>
        public int OrdinalNumber { get; set; }

        /// <summary>
        /// Nazwa pliku części paczki.
        /// </summary>
        public string PartName { get; set; }

        /// <summary>
        /// Metoda HTTP, której należy użyć przy pobieraniu pliku.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Adres URL, pod który należy wysłać żądanie pobrania.
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// Rozmiar części paczki w bajtach. Maksymalny rozmiar 50 MiB.
        /// </summary>
        public long PartSize { get; set; }

        /// <summary>
        /// Skrót SHA256 pliku części paczki, zakodowany w formacie Base64.
        /// </summary>
        public string PartHash { get; set; }

        /// <summary>
        /// Rozmiar zaszyfrowanej części paczki w bajtach.
        /// </summary>
        public long EncryptedPartSize { get; set; }

        /// <summary>
        /// Skrót SHA256 zaszyfrowanej części paczki, zakodowany w formacie Base64.
        /// </summary>
        public string EncryptedPartHash { get; set; }

        /// <summary>
        /// Moment wygaśnięcia linku do pobrania części.
        /// </summary>
        public DateTimeOffset ExpirationDate { get; set; }
    }
}
