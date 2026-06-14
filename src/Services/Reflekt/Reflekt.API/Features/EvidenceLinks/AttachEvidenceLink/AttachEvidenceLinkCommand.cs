using FluentValidation;

namespace Reflekt.API.Features.EvidenceLinks.AttachEvidenceLink;

public sealed record AttachEvidenceLinkCommand(int SprintId, int CardId, string Url);

public sealed class AttachEvidenceLinkValidator : Validator<AttachEvidenceLinkCommand>
{
    public AttachEvidenceLinkValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Url must be a valid absolute HTTP or HTTPS URL.");
    }
}
