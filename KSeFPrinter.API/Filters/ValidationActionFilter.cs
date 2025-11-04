using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KSeFPrinter.API.Filters;

/// <summary>
/// Action filter który automatycznie waliduje request models przy użyciu FluentValidation
/// </summary>
public class ValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationActionFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Iteruj przez wszystkie parametry akcji
        foreach (var parameter in context.ActionArguments)
        {
            if (parameter.Value == null) continue;

            var parameterType = parameter.Value.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(parameterType);

            // Pobierz validator dla tego typu
            var validator = _serviceProvider.GetService(validatorType) as IValidator;
            if (validator != null)
            {
                var validationContext = new ValidationContext<object>(parameter.Value);
                var validationResult = await validator.ValidateAsync(validationContext);

                if (!validationResult.IsValid)
                {
                    // Zwróć strukturalną odpowiedź z błędami walidacji
                    context.Result = new BadRequestObjectResult(new
                    {
                        statusCode = 400,
                        message = "Validation failed",
                        validationErrors = validationResult.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()
                            ),
                        timestamp = DateTime.UtcNow,
                        path = context.HttpContext.Request.Path.ToString()
                    });
                    return;
                }
            }
        }

        await next();
    }
}
