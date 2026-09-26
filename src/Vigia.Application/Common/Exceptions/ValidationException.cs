namespace Vigia.Application.Common.Exceptions;

/// <summary>
/// Input failed validation. Carries errors per field so clients (UI, CLI, Terraform) can point at the exact field.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Creates the exception from errors grouped by field path.</summary>
    /// <param name="errors">Field path (for example <c>config.url</c>) to error messages.</param>
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    /// <summary>Creates the exception for a single field.</summary>
    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    /// <summary>Field path to error messages.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
