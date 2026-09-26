using Mediator;

namespace Vigia.Application.Alerts.GetAlert;

/// <summary>
/// Gets an alert by id.
/// </summary>
/// <param name="Id">Alert id.</param>
public sealed record GetAlertQuery(Guid Id) : IQuery<AlertDto>;
