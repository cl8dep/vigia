using Mediator;

namespace Vigia.Application.StatusPages.UpdateStatusPage;

/// <summary>
/// Replaces a status page's definition.
/// </summary>
/// <param name="Slug">Page to update.</param>
/// <param name="Spec">New definition.</param>
public sealed record UpdateStatusPageCommand(string Slug, StatusPageSpec Spec) : ICommand<StatusPageDto>;
