// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Sdk.Tools.Cli.Models.Responses;

namespace Azure.Sdk.Tools.Cli.Tools.CIChecks
{
    public interface ICICheck
    {
        string Name { get; }
        string Description { get; }
        Task<CICheckResponse> RunCheckAsync(string rootPath, CancellationToken cancellationToken = default);
    }

    public interface ICICheckRunner
    {
        Task<CICheckSuiteResponse> RunChecksAsync(string rootPath, IEnumerable<string> checkNames, CancellationToken cancellationToken = default);
        IReadOnlyList<ICICheck> AvailableChecks { get; }
    }
}
