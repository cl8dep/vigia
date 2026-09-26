namespace Vigia.Api.Requests;

/// <summary>
/// Body of <c>POST /worker/v1/heartbeat</c>.
/// </summary>
/// <param name="Version">Worker software version.</param>
public sealed record HeartbeatRequest(string? Version);
