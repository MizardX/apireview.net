﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ApiReviewDotNet.Data;

namespace ApiReviewDotNet.Services.GitHub
{
    public interface IGitHubManager
    {
        Task<IReadOnlyList<ApiReviewFeedback>> GetFeedbackAsync(DateTimeOffset start, DateTimeOffset end);
        Task<IReadOnlyList<ApiReviewIssue>> GetIssuesAsync();
    }
}
