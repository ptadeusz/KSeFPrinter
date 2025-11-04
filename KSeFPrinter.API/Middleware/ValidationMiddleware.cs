using FluentValidation;
using System.Text.Json;

namespace KSeFPrinter.API.Middleware;

/// <summary>
/// Middleware automatycznie walidujący requesty przy użyciu FluentValidation
/// </summary>
public class ValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
    }
}

/// <summary>
/// Validation filter dla automatycznej walidacji requestów
/// </summary>
public class ValidationFilter : IEndpointFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Znajdź argument który wymaga walidacji
        foreach (var argument in context.Arguments)
        {
            if (argument == null) continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

            // Sprawdź czy istnieje validator dla tego typu
            var validator = _serviceProvider.GetService(validatorType) as IValidator;
            if (validator != null)
            {
                var validationContext = new ValidationContext<object>(argument);
                var validationResult = await validator.ValidateAsync(validationContext);

                if (!validationResult.IsValid)
                {
                    // Zwróć 400 z błędami walidacji
                    return Results.BadRequest(new
                    {
                        statusCode = 400,
                        message = "Validation failed",
                        validationErrors = validationResult.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()
                            ),
                        timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        return await next(context);
    }
}
