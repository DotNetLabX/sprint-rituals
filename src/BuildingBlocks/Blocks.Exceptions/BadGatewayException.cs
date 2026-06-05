namespace Blocks.Exceptions;

public class BadGatewayException(string message) : HttpException(502, message);
