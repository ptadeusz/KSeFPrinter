namespace KSeFPrinter.API.Services;

/// <summary>
/// Interfejs dla przyszłej integracji z KSeF Connector API (wariant B)
/// OBECNIE NIEUŻYWANY - zarezerwowany dla przyszłej funkcjonalności
/// </summary>
public interface IKSeFConnectorService
{
    /// <summary>
    /// Pobiera fakturę XML z systemu KSeF przez KSeF Connector
    /// </summary>
    /// <param name="ksefNumber">Numer KSeF</param>
    /// <returns>Zawartość XML faktury</returns>
    Task<string> GetInvoiceXmlAsync(string ksefNumber);

    /// <summary>
    /// Wysyła fakturę do systemu KSeF przez KSeF Connector
    /// </summary>
    /// <param name="xmlContent">Zawartość XML faktury</param>
    /// <returns>Numer KSeF nadany przez system</returns>
    Task<string> SubmitInvoiceAsync(string xmlContent);

    /// <summary>
    /// Pobiera listę faktur z systemu KSeF
    /// </summary>
    /// <param name="dateFrom">Data od</param>
    /// <param name="dateTo">Data do</param>
    /// <returns>Lista numerów KSeF</returns>
    Task<List<string>> GetInvoiceListAsync(DateTime dateFrom, DateTime dateTo);
}

// TODO: Implementacja dla wariantu B
// public class KSeFConnectorService : IKSeFConnectorService
// {
//     private readonly HttpClient _httpClient;
//     private readonly string _ksefConnectorBaseUrl;
//
//     public KSeFConnectorService(HttpClient httpClient, IConfiguration configuration)
//     {
//         _httpClient = httpClient;
//         _ksefConnectorBaseUrl = configuration["KSeFConnector:BaseUrl"] ?? "http://localhost:5000";
//     }
//
//     public async Task<string> GetInvoiceXmlAsync(string ksefNumber)
//     {
//         var response = await _httpClient.GetAsync($"{_ksefConnectorBaseUrl}/api/invoices/{ksefNumber}/xml");
//         response.EnsureSuccessStatusCode();
//         return await response.Content.ReadAsStringAsync();
//     }
//
//     // ... pozostałe metody
// }
