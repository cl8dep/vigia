namespace Vigia.Domain.Tags;

/// <summary>
/// One entry in the <see cref="SystemTags"/> catalog.
/// </summary>
/// <param name="Key">Key in the reserved <c>vigia:</c> namespace.</param>
/// <param name="Assignment">Who assigns it.</param>
/// <param name="ValueKind">What its value may be.</param>
/// <param name="AppliesTo">Entities it may appear on.</param>
/// <param name="AllowedValues">Allowed values when <paramref name="ValueKind"/> is <see cref="TagValueKind.Vocabulary"/>.</param>
public sealed record SystemTagDefinition(
    string Key,
    TagAssignment Assignment,
    TagValueKind ValueKind,
    IReadOnlySet<TaggedEntity> AppliesTo,
    IReadOnlyList<string>? AllowedValues = null);
