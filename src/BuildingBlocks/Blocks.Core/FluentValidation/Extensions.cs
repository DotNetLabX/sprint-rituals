using FluentValidation;

namespace Blocks.Core.FluentValidation;

public static class Extensions
{
    public static IRuleBuilderOptions<T, string> NotEmptyWithMessage<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        string propertyName)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage($"'{propertyName}' must not be empty.");
    }

    public static IRuleBuilderOptions<T, string> MaximumLengthWithMessage<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        int maxLength,
        string propertyName)
    {
        return ruleBuilder
            .MaximumLength(maxLength)
            .WithMessage($"'{propertyName}' must not exceed {maxLength} characters.");
    }
}
