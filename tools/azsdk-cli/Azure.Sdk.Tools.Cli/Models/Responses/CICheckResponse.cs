// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Azure.Sdk.Tools.Cli.Models.Responses
{
    public class CICheckResponse : Response
    {
        [JsonPropertyName("checkName")]
        public string CheckName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public CICheckStatus Status { get; set; } = CICheckStatus.Unknown;

        [JsonPropertyName("details")]
        public List<CICheckDetail> Details { get; set; } = [];

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("filesChecked")]
        public int FilesChecked { get; set; }

        [JsonPropertyName("issuesFound")]
        public int IssuesFound { get; set; }
    }

    public class CICheckDetail
    {
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("line")]
        public int? Line { get; set; }

        [JsonPropertyName("column")]
        public int? Column { get; set; }

        [JsonPropertyName("severity")]
        public CICheckSeverity Severity { get; set; } = CICheckSeverity.Info;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("rule")]
        public string Rule { get; set; } = string.Empty;
    }

    public class CICheckSuiteResponse : Response
    {
        [JsonPropertyName("checks")]
        public List<CICheckResponse> Checks { get; set; } = [];

        [JsonPropertyName("overallStatus")]
        public CICheckStatus OverallStatus { get; set; } = CICheckStatus.Unknown;

        [JsonPropertyName("totalIssues")]
        public int TotalIssues { get; set; }

        [JsonPropertyName("totalFilesChecked")]
        public int TotalFilesChecked { get; set; }

        [JsonPropertyName("executionTime")]
        public TimeSpan ExecutionTime { get; set; }
    }

    public enum CICheckStatus
    {
        Unknown,
        Pass,
        Warning,
        Fail,
        Skipped,
        Error
    }

    public enum CICheckSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
