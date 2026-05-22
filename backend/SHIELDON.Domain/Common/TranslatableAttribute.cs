namespace SHIELDON.Domain.Common;

/// <summary>
/// Attribute to mark string properties that should be automatically translated by the interceptor.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TranslatableAttribute : Attribute
{
}
