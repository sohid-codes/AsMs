namespace AsMs.Application.Services;

public abstract class ApplicationExceptionBase(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class ValidationException(string message) : ApplicationExceptionBase(message, 400);
public sealed class NotFoundException(string message) : ApplicationExceptionBase(message, 404);
public sealed class ConflictException(string message) : ApplicationExceptionBase(message, 409);
public sealed class ForbiddenException(string message) : ApplicationExceptionBase(message, 403);
