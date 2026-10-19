namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;

/// <summary>
/// Describes the administrative removal of one persisted MOPR user.
/// </summary>
/// <param name="Id">The positive persistent user identifier.</param>
/// <param name="RowVersion">The optimistic-concurrency token loaded with the user.</param>
public sealed record UserAdministrationDeleteRequest(int Id, byte[] RowVersion);