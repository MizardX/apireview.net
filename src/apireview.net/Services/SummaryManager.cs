﻿using ApiReviewDotNet.Data;
using ApiReviewDotNet.Services.GitHub;
using ApiReviewDotNet.Services.YouTube;

namespace ApiReviewDotNet.Services;

public sealed class SummaryManager
{
    // We sometimes post the comments from the last issue slightly after the stream
    // has ended. To account for that, we'll give us 15 minutes extra.
    private static readonly TimeSpan _extraTimeAfterStreamEnded = TimeSpan.FromMinutes(15);

    private readonly YouTubeManager _youTubeManager;
    private readonly GitHubManager _gitHubManager;

    public SummaryManager(YouTubeManager youTubeManager, GitHubManager gitHubManager)
    {
        _youTubeManager = youTubeManager;
        _gitHubManager = gitHubManager;
    }

    public async Task<ApiReviewSummary> GetSummaryAsync(RepositoryGroup repositoryGroup, DateTimeOffset start, DateTimeOffset end)
    {
        var items = await _gitHubManager.GetFeedbackAsync(repositoryGroup.Repos, start, end);
        return CreateSummary(repositoryGroup, null, items);
    }

    public async Task<ApiReviewSummary?> GetSummaryAsync(RepositoryGroup repositoryGroup, string videoId)
    {
        var video = await _youTubeManager.GetVideoAsync(videoId);
        if (video is null)
            return null;

        var start = video.StartDateTime;
        var end = video.EndDateTime + _extraTimeAfterStreamEnded;
        var items = await _gitHubManager.GetFeedbackAsync(repositoryGroup.Repos, start, end);
        return CreateSummary(repositoryGroup, video, items);
    }

    private static ApiReviewSummary CreateSummary(RepositoryGroup repositoryGroup, ApiReviewVideo? video, IReadOnlyList<ApiReviewItem> items)
    {
        if (items.Count == 0)
        {
            return new ApiReviewSummary(
                repositoryGroup: repositoryGroup.Name,
                video: video,
                items: Array.Empty<ApiReviewItem>()
            );
        }
        else
        {
            var reviewStart = video is null
                                ? items.OrderBy(i => i.FeedbackDateTime).Select(i => i.FeedbackDateTime).First()
                                : video.StartDateTime;

            var reviewEnd = video is null
                                ? items.OrderBy(i => i.FeedbackDateTime).Select(i => i.FeedbackDateTime).Last()
                                : video.EndDateTime + _extraTimeAfterStreamEnded;

            for (var i = 0; i < items.Count; i++)
            {
                var current = items[i];

                if (video is not null)
                {
                    var wasDuringReview = reviewStart <= current.FeedbackDateTime && current.FeedbackDateTime <= reviewEnd;
                    if (!wasDuringReview)
                        continue;
                }

                var previous = i == 0 ? null : items[i - 1];

                TimeSpan timeCode;

                if (previous is null || video is null)
                {
                    timeCode = TimeSpan.Zero;
                }
                else
                {
                    timeCode = (previous.FeedbackDateTime - video.StartDateTime).Add(TimeSpan.FromSeconds(10));
                    var videoDuration = video.EndDateTime - video.StartDateTime;
                    if (timeCode >= videoDuration)
                        timeCode = items[i - 1].TimeCode;
                }

                current.TimeCode = timeCode;
            }

            return new ApiReviewSummary(
                repositoryGroup: repositoryGroup.Name,
                video: video,
                items: items
            );
        }
    }
}
