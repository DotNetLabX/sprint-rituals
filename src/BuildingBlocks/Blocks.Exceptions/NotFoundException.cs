namespace Blocks.Exceptions;

public class NotFoundException(string message) : HttpException(404, message);
