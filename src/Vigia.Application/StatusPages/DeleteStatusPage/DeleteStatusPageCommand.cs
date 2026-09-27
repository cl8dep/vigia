using Mediator;

namespace Vigia.Application.StatusPages.DeleteStatusPage;

/// <summary>
/// Deletes a status page. Its public URL stops working; services are untouched.
/// </summary>
/// <param name="Slug">Page to delete.</param>
public sealed record DeleteStatusPageCommand(string Slug) : ICommand;
