namespace SHIELDON.Domain.Exceptions;

/// <summary>
/// Base class for all SHIELDON domain-specific exceptions.
/// Thrown when a business rule violation occurs.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a requested resource is not found.
/// Maps to HTTP 404.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with identifier '{key}' was not found.") { }
}

/// <summary>
/// Thrown when a business rule or validation constraint is violated.
/// Maps to HTTP 400.
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a user attempts an action they are not authorized to perform.
/// Maps to HTTP 403.
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message) { }
}

/// <summary>
/// Thrown when a resource already exists and a duplicate is not allowed.
/// Maps to HTTP 409.
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}
