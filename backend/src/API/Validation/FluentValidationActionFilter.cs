using Demo.Api.Common;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Demo.Api.Validation;

/// <summary>
/// Chạy FluentValidation cho các action argument đã bind và trả về ApiErrorResponse thống nhất khi request không hợp lệ.
/// </summary>
public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    /// <summary>
    /// Validate argument trước khi chuyển quyền xử lý cho action tiếp theo.
    /// <param name="context">Context chứa argument và metadata của action.</param>
    /// <param name="next">Delegate gọi action khi validation thành công.</param>
    /// <returns>Task hoàn tất sau khi action được thực thi hoặc response lỗi được tạo.</returns>
    /// </summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validators = context.HttpContext.RequestServices.GetServices(validatorType)
                .OfType<IValidator>()
                .ToArray();

            if (validators.Length == 0)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                if (!result.IsValid)
                {
                    failures.AddRange(result.Errors);
                }
            }

            var firstFailure = failures
                .Where(failure => failure is not null)
                .FirstOrDefault();

            if (firstFailure is null)
            {
                continue;
            }

            context.Result = new BadRequestObjectResult(new ApiErrorResponse(
                ValidationErrorCodeResolver.Resolve(argument.GetType()),
                firstFailure.ErrorMessage));
            return;
        }

        await next();
    }
}
