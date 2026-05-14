using FluentValidation;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using MediatR;

namespace GymApp.Common.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);

        var validationFailures = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Per docs/PRD_TRAINEE_ME_ENDPOINTS.md, the canonical code for validation failures is
        // "VALIDATION_ERROR". Field name lives in Meta so clients can still do per-field UI.
        var errors = validationFailures
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .Select(f => new Error(
                "VALIDATION_ERROR",
                f.ErrorMessage,
                new { field = f.PropertyName }))
            .ToList();

        if (errors.Count != 0)
        {
            throw new Exceptions.ValidationException(errors);
        }

        return await next();
    }
}
