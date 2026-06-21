using FastEndpoints;
using FluentValidation;

namespace Poker.API.Features.Items.AddSessionItem;

public class AddSessionItemCommand
{
    public Guid Id { get; set; }

    public string Label { get; set; } = default!;
}

public class AddSessionItemCommandValidator : Validator<AddSessionItemCommand>
{
    public AddSessionItemCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(500);
    }
}
