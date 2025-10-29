namespace KSeF.Client.DI;

public class CryptographyClientOptions
{
    public WarmupMode WarmupOnStart { get; set; } = WarmupMode.Blocking;
}

/// <summary>
/// Określa sposób wykonania procesu pobrania certyfikatów publicznych:
/// - Disabled: nie uruchamiaj w tle; aplikacja startuje dalej
/// - NonBlocking: uruchom w tle; aplikacja startuje dalej
/// - Blocking: uruchom i czekaj na zakończenie; jeśli nie wyjdzie, aplikacja się nie uruchomi
/// </summary>
/// <remarks>Określa jak ma zostać zainicjalizowana aplikacja oraz jakie ma być jej zachowanie w zależności od przekazanej opcji oraz wyniku pobrania certyfikatów publicznych.</remarks>
public enum WarmupMode
{
    Disabled,       // Nie uruchamiaj w tle; aplikacja startuje dalej
    NonBlocking,    // Uruchom w tle; aplikacja startuje dalej
    Blocking        // Uruchom i czekaj na zakończenie; jeśli nie wyjdzie, aplikacja się nie uruchomi
}