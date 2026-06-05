namespace Blocks.Exceptions;

public class ConflictException(string message) : HttpException(409, message);
