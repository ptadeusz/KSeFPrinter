namespace KSeF.Client.Tests.Utils;

public static class AsyncPollingUtils
{
    /// <summary>
    /// Decyzja dotycząca ograniczeń (rate limit).
    /// Gdy IsRateLimited = true:
    /// - bieżąca próba nie jest liczona,
    /// - używany jest DelayOverride (jeśli nie null) jako czas oczekiwania przed kolejną próbą.
    /// </summary>
    public readonly record struct RateLimitDecision(bool IsRateLimited, TimeSpan? DelayOverride = null);

    /// <summary>
    /// Odpytywanie poprzez wywoływanie <paramref name="action"/> aż do spełnienia warunku <paramref name="condition"/> lub osiągnięcia limitu <paramref name="maxAttempts"/>.
    /// Używa stałego opóźnienia między próbami.
    /// Zgłasza TimeoutException, gdy warunek nie zostanie spełniony w ramach maxAttempts.
    /// Wyjątki zgłaszane przez <paramref name="action"/> są ponawiane, gdy <paramref name="shouldRetryOnException"/> zwraca true.
    /// Obsługa 429 (rate limit): przekaż <paramref name="rateLimitOnException"/> i/lub <paramref name="rateLimitOnResult"/> by nie naliczać prób i zwiększyć czas oczekiwania.
    /// </summary>
    public static async Task<TResult> PollAsync<TResult>(
        Func<Task<TResult>> action,
        Func<TResult, bool> condition,
        string description = "",
        TimeSpan? delay = null,
        int maxAttempts = 60,
        Func<Exception, bool>? shouldRetryOnException = null,
        CancellationToken cancellationToken = default,
        Func<Exception, RateLimitDecision>? rateLimitOnException = null,
        Func<TResult, RateLimitDecision>? rateLimitOnResult = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        if (condition is null) throw new ArgumentNullException(nameof(condition));
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        var defaultWait = delay ?? TimeSpan.FromSeconds(1);
        shouldRetryOnException ??= static ex => ex is not OperationCanceledException and not TaskCanceledException;

        Exception? lastError = null;
        TResult lastResult = default!;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool doNotCountAttempt = false;
            TimeSpan waitThisTime = defaultWait;

            try
            {
                lastResult = await action().ConfigureAwait(false);
                if (condition(lastResult))
                    return lastResult;

                // Możliwość oznaczenia rate limitu na podstawie wyniku (jeśli wynik niesie takie informacje)
                if (rateLimitOnResult is not null)
                {
                    var rl = rateLimitOnResult(lastResult);
                    if (rl.IsRateLimited)
                    {
                        doNotCountAttempt = true;
                        if (rl.DelayOverride is TimeSpan d) waitThisTime = d;
                    }
                }

                lastError = null; // resetuje błąd po udanym wywołaniu
            }
            catch (Exception ex) when (shouldRetryOnException(ex))
            {
                // Decyzja rate limit na podstawie wyjątku (np. HTTP 429)
                if (rateLimitOnException is not null)
                {
                    var rl = rateLimitOnException(ex);
                    if (rl.IsRateLimited)
                    {
                        doNotCountAttempt = true;
                        if (rl.DelayOverride is TimeSpan d) waitThisTime = d;
                    }
                }

                lastError = ex;
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(waitThisTime, cancellationToken).ConfigureAwait(false);

                // Nie naliczaj tej próby, jeśli trafiliśmy na rate limit
                if (doNotCountAttempt)
                {
                    attempt--;
                }
            }
        }

        throw lastError is not null
            ? new TimeoutException($"{description} {Environment.NewLine}Nie spełniono warunku w {maxAttempts} próbach. Ostatni błąd: {lastError.Message}", lastError)
            : new TimeoutException($"{description} {Environment.NewLine}Nie spełniono warunku w {maxAttempts} próbach.");
    }

    /// <summary>
    /// Odpytywanie z wykładniczym backoffem.
    /// Obsługa 429 (rate limit): przekaż <paramref name="rateLimitOnException"/> i/lub <paramref name="rateLimitOnResult"/> by nie naliczać prób i zwiększyć czas oczekiwania.
    /// </summary>
    public static async Task<TResult> PollWithBackoffAsync<TResult>(
        Func<Task<TResult>> action,
        Func<TResult, bool> condition,
        TimeSpan initialDelay,
        TimeSpan maxDelay,
        int maxAttempts = 60,
        double backoffFactor = 2.0,
        bool jitter = true,
        Func<Exception, bool>? shouldRetryOnException = null,
        CancellationToken cancellationToken = default,
        Func<Exception, RateLimitDecision>? rateLimitOnException = null,
        Func<TResult, RateLimitDecision>? rateLimitOnResult = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        if (condition is null) throw new ArgumentNullException(nameof(condition));
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        if (initialDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(initialDelay));
        if (maxDelay < initialDelay) throw new ArgumentOutOfRangeException(nameof(maxDelay));
        if (backoffFactor < 1.0) throw new ArgumentOutOfRangeException(nameof(backoffFactor));

        shouldRetryOnException ??= static ex => ex is not OperationCanceledException and not TaskCanceledException;

        var currentDelay = initialDelay;
        var rng = jitter ? new Random() : null;

        Exception? lastError = null;
        TResult lastResult = default!;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool doNotCountAttempt = false;
            TimeSpan waitThisTime = currentDelay;

            try
            {
                lastResult = await action().ConfigureAwait(false);
                if (condition(lastResult))
                    return lastResult;

                if (rateLimitOnResult is not null)
                {
                    var rl = rateLimitOnResult(lastResult);
                    if (rl.IsRateLimited)
                    {
                        doNotCountAttempt = true;
                        if (rl.DelayOverride is TimeSpan d) waitThisTime = d;
                    }
                }

                lastError = null;
            }
            catch (Exception ex) when (shouldRetryOnException(ex))
            {
                if (rateLimitOnException is not null)
                {
                    var rl = rateLimitOnException(ex);
                    if (rl.IsRateLimited)
                    {
                        doNotCountAttempt = true;
                        if (rl.DelayOverride is TimeSpan d) waitThisTime = d;
                    }
                }

                lastError = ex;
            }

            if (attempt < maxAttempts)
            {
                var wait = waitThisTime;
                if (!doNotCountAttempt && rng is not null && wait == currentDelay)
                {
                    // Pełny jitter w zakresie [0.5x, 1.5x] tylko dla zwykłego backoffu
                    var ms = Math.Max(1, (int)wait.TotalMilliseconds);
                    var jitterMs = rng.Next((int)(ms * 0.5), (int)(ms * 1.5) + 1);
                    wait = TimeSpan.FromMilliseconds(jitterMs);
                }

                await Task.Delay(wait, cancellationToken).ConfigureAwait(false);

                // Postęp backoffu tylko dla zwykłej ścieżki (nie dla rate limit override)
                if (!doNotCountAttempt)
                {
                    var nextMs = Math.Min(maxDelay.TotalMilliseconds, currentDelay.TotalMilliseconds * backoffFactor);
                    currentDelay = TimeSpan.FromMilliseconds(nextMs);
                }
                else
                {
                    // Nie naliczaj tej próby
                    attempt--;
                }
            }
        }

        throw lastError is not null
            ? new TimeoutException($"Nie spełniono warunku w {maxAttempts} próbach. Ostatni błąd: {lastError.Message}", lastError)
            : new TimeoutException($"Nie spełniono warunku w {maxAttempts} próbach.");
    }

    /// <summary>
    /// Przeciążenie do bezpośredniego odpytywania warunku typu bool.
    /// </summary>
    public static Task PollAsync(
        Func<Task<bool>> check,
        string description = "",
        TimeSpan? delay = null,
        int maxAttempts = 60,
        Func<Exception, bool>? shouldRetryOnException = null,
        CancellationToken cancellationToken = default,
        Func<Exception, RateLimitDecision>? rateLimitOnException = null,
        Func<bool, RateLimitDecision>? rateLimitOnResult = null)
        => PollAsync(async () => await check().ConfigureAwait(false),
                     result => result,                     
                     description,
                     delay,
                     maxAttempts,
                     shouldRetryOnException,
                     cancellationToken,
                     rateLimitOnException,
                     rateLimitOnResult);
}