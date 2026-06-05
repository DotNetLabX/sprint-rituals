namespace Blocks.Exceptions;

public class ForbiddenException(string message) : HttpException(403, message);
