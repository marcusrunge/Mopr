using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Miras.ViewModels;

/// <summary>
/// Represents one structured MIRAS issue together with its affected repository location.
/// </summary>
public sealed class MirasIssueItemViewModel(Guid id, MirasIssueType issueType, MirasIssueState issueState, MirasRecommendedAction recommendedAction, string title, string description, bool canDeferForSession, int? repositoryLocationId, string repositoryName, string repositoryPath, bool? isRepositoryEnabled, bool? isRepositoryDefault)
{
    public bool CanDeferForSession { get; } = canDeferForSession;
    public string Description { get; } = description;
    public Guid Id { get; } = id;
    public bool? IsRepositoryDefault { get; } = isRepositoryDefault;
    public bool? IsRepositoryEnabled { get; } = isRepositoryEnabled;
    public MirasIssueState IssueState { get; } = issueState;
    public MirasIssueType IssueType { get; } = issueType;
    public MirasRecommendedAction RecommendedAction { get; } = recommendedAction;
    public int? RepositoryLocationId { get; } = repositoryLocationId;
    public string RepositoryName { get; } = repositoryName;
    public string RepositoryPath { get; } = repositoryPath;
    public string Title { get; } = title;
}
