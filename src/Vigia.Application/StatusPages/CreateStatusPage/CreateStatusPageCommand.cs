using Mediator;

namespace Vigia.Application.StatusPages.CreateStatusPage;

/// <summary>
/// Creates a status page.
/// </summary>
/// <param name="Slug">Stable identity and public URL segment.</param>
/// <param name="Spec">Page definition.</param>
public sealed record CreateStatusPageCommand(string Slug, StatusPageSpec Spec) : ICommand<StatusPageDto>;
