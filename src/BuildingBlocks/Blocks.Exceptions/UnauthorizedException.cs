namespace Blocks.Exceptions;

public class UnauthorizedException(string message) : HttpException(401, message);
