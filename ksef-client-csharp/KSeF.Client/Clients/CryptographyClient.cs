using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Http;

namespace KSeF.Client.Clients;

public class CryptographyClient : ICryptographyClient
{
    private readonly IRestClient _restClient;

    public CryptographyClient(IRestClient restClient)
    {
        _restClient = restClient;
    }

    /// <inheritdoc />
    public async Task<ICollection<PemCertificateInfo>> GetPublicCertificatesAsync(CancellationToken cancellationToken)
    {
        return await _restClient.SendAsync<ICollection<PemCertificateInfo>, string>(HttpMethod.Get,
                                                                      "/api/v2/security/public-key-certificates",
                                                                      default,
                                                                      default,
                                                                      RestClient.DefaultContentType,
                                                                      cancellationToken).ConfigureAwait(false);
    }
}

