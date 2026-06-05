namespace Blocks.Exceptions;

public abstract class HttpException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
