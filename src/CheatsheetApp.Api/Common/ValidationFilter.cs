using FluentValidation;

namespace CheatsheetApp.Api.Common;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is not null)
        {
            var model = context.Arguments.OfType<T>().FirstOrDefault();
            if (model is null)
                return Results.BadRequest(ApiResponse<object>.Fail("Invalid request body."));

            var result = await validator.ValidateAsync(model);
            if (!result.IsValid)
                return Results.BadRequest(ApiResponse<object>.Fail(
                    string.Join("; ", result.Errors.Select(e => e.ErrorMessage))));
        }
        return await next(context);
    }
}
