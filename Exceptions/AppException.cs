namespace HouseRentalAPI.Exceptions;

/// <summary>
/// Lỗi nghiệp vụ có chủ đích, được ExceptionHandlingMiddleware chuyển thành HTTP status và trả về ErrorResponse.
/// </summary>
public abstract class AppException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class BadRequestException(string message) : AppException(message, StatusCodes.Status400BadRequest);

public class UnauthorizedException(string message) : AppException(message, StatusCodes.Status401Unauthorized);

public class ForbiddenException(string message) : AppException(message, StatusCodes.Status403Forbidden);

public class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound);

public class ConflictException(string message) : AppException(message, StatusCodes.Status409Conflict);
