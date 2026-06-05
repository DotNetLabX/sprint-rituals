namespace Blocks.Exceptions;

public class BadRequestException(string message) : HttpException(400, message);
