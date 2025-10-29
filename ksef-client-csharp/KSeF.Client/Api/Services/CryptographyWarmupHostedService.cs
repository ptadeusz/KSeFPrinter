using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.DI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KSeF.Client.Api.Services;

public sealed class CryptographyWarmupHostedService : IHostedService
{
    private readonly ICryptographyService _cryptographyService;
    private readonly IOptions<CryptographyClientOptions> _cryptographyClientOptions;

    public CryptographyWarmupHostedService(
        ICryptographyService cryptographyService,
        IOptions<CryptographyClientOptions> cryptographyClientOptions)
    {
        _cryptographyService = cryptographyService;
        _cryptographyClientOptions = cryptographyClientOptions;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        switch (_cryptographyClientOptions.Value.WarmupOnStart)
        {
            case WarmupMode.Disabled:
                return Task.CompletedTask;
            case WarmupMode.NonBlocking:
                _ = Task.Run(() => SafeWarmup(cancellationToken), CancellationToken.None);
                return Task.CompletedTask;
            case WarmupMode.Blocking:
                return SafeWarmup(cancellationToken);
            default:
                return Task.CompletedTask;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SafeWarmup(CancellationToken cancellationToken)
    {
        try
        {
            await _cryptographyService.WarmupAsync(cancellationToken);
        }
        catch (Exception)
        {
            if (_cryptographyClientOptions.Value.WarmupOnStart == WarmupMode.Blocking)
            {
                throw;
            }
        }
    }
}
