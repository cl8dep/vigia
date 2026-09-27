namespace Vigia.Application.StatusPages;

/// <summary>
/// Everything that defines a status page except its slug. Shared by create and update (full replace).
/// </summary>
public record StatusPageSpec
{
    /// <summary>Public title. Defaults to the slug.</summary>
    public string? Title { get; init; }

    /// <summary>Public text under the title.</summary>
    public string? Description { get; init; }

    /// <summary>Tags.</summary>
    public IReadOnlyDictionary<string, string?>? Tags { get; init; }

    /// <summary>Components in display order.</summary>
    public IReadOnlyList<ComponentSpec>? Components { get; init; }
}
