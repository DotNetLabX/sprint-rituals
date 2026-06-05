using Blocks.Exceptions;

namespace Blocks.Core;

public static class GuardExtensions
{
    public static T OrThrowNotFound<T>(this T? value, string message = "The requested resource was not found.") where T : class
    {
        if (value is null)
            throw new NotFoundException(message);
        return value;
    }
}
