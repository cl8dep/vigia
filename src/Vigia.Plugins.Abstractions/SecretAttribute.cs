namespace Vigia.Plugins;

/// <summary>
/// Marks a field as secret: encrypted at rest and never returned by the API.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SecretAttribute : Attribute;
