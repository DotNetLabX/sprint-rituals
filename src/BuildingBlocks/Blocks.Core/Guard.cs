using Blocks.Exceptions;

namespace Blocks.Core;

public static class Guard
{
    public static T NotFound<T>(T? value, string message = "The requested resource was not found.") where T : class
    {
        if (value is null)
            throw new NotFoundException(message);
        return value;
    }

    public static void ThrowIfNullOrWhiteSpace(string? value, string message = "Value cannot be null or whitespace.")
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BadRequestException(message);
    }

    public static void ThrowIfFalse(bool condition, string message)
    {
        if (!condition)
            throw new BadRequestException(message);
    }
}
