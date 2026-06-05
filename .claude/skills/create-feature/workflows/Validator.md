# Create Validator

## Inputs
- Command/request type name and its properties
- Validation rules from the plan step (required fields, format constraints, business rules)
- Framework (FastEndpoints or FluentValidation standalone)

## Outputs
- FastEndpoints: `{FeatureName}CommandValidator` class co-located with the command
- Other: `{FeatureName}Validator.cs` alongside the command file

## Gate
Proceed only after: command/request type is defined and its properties are known.

## Framework Variants

### FastEndpoints: `Validator<T>`

Co-located with command. Auto-discovered by FastEndpoints:

```csharp
public class {FeatureName}CommandValidator : Validator<{FeatureName}Command>
{
    public {FeatureName}CommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmptyWithMessage(nameof({FeatureName}Command.Name))
            .MaximumLengthWithMessage(MaxLength.C64, nameof({FeatureName}Command.Name));
    }
}
```

**Optional:** if the service defines a custom base validator (check service CLAUDE.md), extend it instead of `Validator<T>`.

### MediatR: `AbstractValidator<T>`

Co-located with command in the same file. Discovered via `AddValidatorsFromAssemblyContaining<T>()`, run by `ValidationBehavior`:

```csharp
public record {FeatureName}Command : ICommand<{ResponseType}>
{
    // properties...
}

public class {FeatureName}CommandValidator : AbstractValidator<{FeatureName}Command>
{
    public {FeatureName}CommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmptyWithMessage(nameof({FeatureName}Command.Name))
            .MaximumLengthWithMessage(MaxLength.C64, nameof({FeatureName}Command.Name));
    }
}
```

## Custom Validation Extensions

**File:** `src/BuildingBlocks/Blocks.Core/FluentValidation/Extensions.cs`

Use these for consistent error messages:
- `NotEmptyWithMessage(propertyName)` — consistent "'PropertyName' must not be empty."
- `MaximumLengthWithMessage(maxLength, propertyName)` — consistent "'PropertyName' must not exceed {max} characters."

## MaxLength Constants

**File:** `src/BuildingBlocks/Blocks.Core/MaxLength.cs`

```csharp
MaxLength.C0, C8, C16, C32, C64, C128, C256, C512, C1024, C2048
```

Use these instead of magic numbers for `HasMaxLength` (EF config) and `MaximumLengthWithMessage` (validator).
