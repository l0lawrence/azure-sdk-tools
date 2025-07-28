// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Sdk.Tools.Cli.Models.Responses;
using Azure.Sdk.Tools.Cli.Tools.CIChecks.Checks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Azure.Sdk.Tools.Cli.Tools.CIChecks
{
    public class CICheckRunner : ICICheckRunner
    {
        private readonly IEnumerable<ICICheck> checks;
        private readonly ILogger<CICheckRunner> logger;

        public CICheckRunner(VerifyReadmeCheck verifyReadmeCheck, ILogger<CICheckRunner> logger)
        {
            // Initialize checks collection with available checks
            this.checks = new List<ICICheck>
            {
                verifyReadmeCheck
                // Add more checks here as they are implemented
            };
            this.logger = logger;
        }

        public IReadOnlyList<ICICheck> AvailableChecks => checks.ToList().AsReadOnly();

        public async Task<CICheckSuiteResponse> RunChecksAsync(string rootPath, IEnumerable<string> checkNames, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new CICheckSuiteResponse
            {
                OverallStatus = CICheckStatus.Pass,
                ExecutionTime = TimeSpan.Zero
            };

            var checkResults = new List<CICheckResponse>();
            var checksToRun = GetChecksToRun(checkNames);

            logger.LogInformation("Running {CheckCount} CI checks on path: {RootPath}", checksToRun.Count, rootPath);

            foreach (var check in checksToRun)
            {
                try
                {
                    logger.LogDebug("Running check: {CheckName}", check.Name);
                    var checkResult = await check.RunCheckAsync(rootPath, cancellationToken);
                    checkResults.Add(checkResult);

                    // Update overall status
                    if (checkResult.Status == CICheckStatus.Fail || checkResult.Status == CICheckStatus.Error)
                    {
                        response.OverallStatus = CICheckStatus.Fail;
                    }
                    else if (checkResult.Status == CICheckStatus.Warning && response.OverallStatus == CICheckStatus.Pass)
                    {
                        response.OverallStatus = CICheckStatus.Warning;
                    }

                    logger.LogDebug("Check {CheckName} completed with status: {Status}", check.Name, checkResult.Status);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error running check {CheckName}", check.Name);
                    var errorResult = new CICheckResponse
                    {
                        CheckName = check.Name,
                        Status = CICheckStatus.Error,
                        Summary = $"Check execution failed: {ex.Message}",
                        Details = [new CICheckDetail
                        {
                            Severity = CICheckSeverity.Error,
                            Message = ex.Message,
                            Rule = "check-execution-error"
                        }]
                    };
                    checkResults.Add(errorResult);
                    response.OverallStatus = CICheckStatus.Fail;
                }
            }

            stopwatch.Stop();
            response.ExecutionTime = stopwatch.Elapsed;
            response.Checks = checkResults;
            response.TotalIssues = checkResults.Sum(c => c.IssuesFound);
            response.TotalFilesChecked = checkResults.Sum(c => c.FilesChecked);

            logger.LogInformation("CI checks completed in {ExecutionTime}ms. Overall status: {OverallStatus}, Total issues: {TotalIssues}",
                response.ExecutionTime.TotalMilliseconds, response.OverallStatus, response.TotalIssues);

            return response;
        }

        private List<ICICheck> GetChecksToRun(IEnumerable<string> checkNames)
        {
            if (!checkNames.Any())
            {
                // Run all checks if none specified
                return checks.ToList();
            }

            var requestedChecks = new List<ICICheck>();
            var availableCheckNames = checks.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var checkName in checkNames)
            {
                var check = checks.FirstOrDefault(c => c.Name.Equals(checkName, StringComparison.OrdinalIgnoreCase));
                if (check != null)
                {
                    requestedChecks.Add(check);
                }
                else
                {
                    logger.LogWarning("Requested check '{CheckName}' not found. Available checks: {AvailableChecks}",
                        checkName, string.Join(", ", availableCheckNames));
                }
            }

            return requestedChecks;
        }
    }
}
