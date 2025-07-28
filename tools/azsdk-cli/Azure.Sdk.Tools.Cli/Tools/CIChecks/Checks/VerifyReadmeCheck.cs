// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Sdk.Tools.Cli.Models.Responses;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Azure.Sdk.Tools.Cli.Tools.CIChecks.Checks
{
    public class VerifyReadmeCheck : ICICheck
    {
        private readonly ILogger<VerifyReadmeCheck> logger;
        private const string DefaultDocWardenVersion = "0.7.2";

        public string Name => "verify-readme";
        public string Description => "Verifies README files using doc-warden tool";

        public VerifyReadmeCheck(ILogger<VerifyReadmeCheck> logger)
        {
            this.logger = logger;
        }

        public async Task<CICheckResponse> RunCheckAsync(string rootPath, CancellationToken cancellationToken = default)
        {
            var response = new CICheckResponse
            {
                CheckName = Name,
                Status = CICheckStatus.Unknown,
                Summary = "README verification check"
            };

            try
            {
                logger.LogInformation("Running README verification check on {RootPath}", rootPath);

                // Find the verify script
                var scriptPath = FindVerifyReadmeScript(rootPath);
                if (string.IsNullOrEmpty(scriptPath))
                {
                    response.Status = CICheckStatus.Error;
                    response.Summary = "Could not find Verify-Readme.ps1 script";
                    response.Details.Add(new CICheckDetail
                    {
                        Severity = CICheckSeverity.Error,
                        Message = "Verify-Readme.ps1 script not found in eng/common/scripts directory",
                        Rule = "script-missing"
                    });
                    return response;
                }

                // Find settings file
                var settingsPath = FindSettingsFile(rootPath);
                if (string.IsNullOrEmpty(settingsPath))
                {
                    response.Status = CICheckStatus.Error;
                    response.Summary = "Could not find README settings file";
                    response.Details.Add(new CICheckDetail
                    {
                        Severity = CICheckSeverity.Error,
                        Message = "README settings file not found. Expected .docsettings.yml or similar",
                        Rule = "settings-missing"
                    });
                    return response;
                }

                // Determine scan paths
                var scanPaths = DetermineScanPaths(rootPath);
                if (!scanPaths.Any())
                {
                    response.Status = CICheckStatus.Skipped;
                    response.Summary = "No directories to scan found";
                    return response;
                }

                // Run the verification
                var result = await RunVerifyReadmeScript(scriptPath, settingsPath, scanPaths, rootPath, cancellationToken);
                
                response.Status = result.Success ? CICheckStatus.Pass : CICheckStatus.Fail;
                response.Summary = result.Summary;
                response.Details = result.Details;
                response.FilesChecked = result.FilesChecked;
                response.IssuesFound = result.Details.Count(d => d.Severity == CICheckSeverity.Error || d.Severity == CICheckSeverity.Critical);

                logger.LogInformation("README verification completed with status: {Status}", response.Status);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running README verification check");
                response.Status = CICheckStatus.Error;
                response.Summary = $"Error running check: {ex.Message}";
                response.Details.Add(new CICheckDetail
                {
                    Severity = CICheckSeverity.Critical,
                    Message = ex.Message,
                    Rule = "execution-error"
                });
                return response;
            }
        }

        private string FindVerifyReadmeScript(string rootPath)
        {
            var possiblePaths = new[]
            {
                Path.Combine(rootPath, "eng", "common", "scripts", "Verify-Readme.ps1"),
                Path.Combine(rootPath, "eng", "scripts", "Verify-Readme.ps1"),
                Path.Combine(rootPath, "scripts", "Verify-Readme.ps1")
            };

            return possiblePaths.FirstOrDefault(File.Exists) ?? string.Empty;
        }

        private string FindSettingsFile(string rootPath)
        {
            var possibleFiles = new[]
            {
                ".docsettings.yml",
                ".docsettings.yaml",
                "eng/.docsettings.yml",
                "eng/.docsettings.yaml",
                "eng/common/.docsettings.yml",
                "eng/common/.docsettings.yaml"
            };

            foreach (var file in possibleFiles)
            {
                var fullPath = Path.Combine(rootPath, file);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return string.Empty;
        }

        private List<string> DetermineScanPaths(string rootPath)
        {
            var scanPaths = new List<string>();
            
            // Common SDK directories to scan
            var commonPaths = new[]
            {
                "sdk",
                "docs",
                "src",
                "lib",
                "packages",
                "tools"
            };

            foreach (var path in commonPaths)
            {
                var fullPath = Path.Combine(rootPath, path);
                if (Directory.Exists(fullPath))
                {
                    scanPaths.Add(fullPath);
                }
            }

            // If no common paths found, scan the root
            if (!scanPaths.Any())
            {
                scanPaths.Add(rootPath);
            }

            return scanPaths;
        }

        private async Task<VerifyReadmeResult> RunVerifyReadmeScript(
            string scriptPath, 
            string settingsPath, 
            List<string> scanPaths, 
            string rootPath,
            CancellationToken cancellationToken)
        {
            var result = new VerifyReadmeResult();
            var output = new StringBuilder();
            var error = new StringBuilder();

            try
            {
                var scanPathsString = string.Join(",", scanPaths);
                var arguments = new[]
                {
                    "-ExecutionPolicy", "Bypass",
                    "-File", $"\"{scriptPath}\"",
                    "-SettingsPath", $"\"{settingsPath}\"",
                    "-ScanPaths", $"\"{scanPathsString}\"",
                    "-RepoRoot", $"\"{rootPath}\"",
                    "-DocWardenVersion", DefaultDocWardenVersion
                };

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "pwsh",
                        Arguments = string.Join(" ", arguments),
                        WorkingDirectory = rootPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                logger.LogDebug("Running command: pwsh {Arguments}", string.Join(" ", arguments));

                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        logger.LogDebug("STDOUT: {Data}", e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                        logger.LogDebug("STDERR: {Data}", e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cancellationToken);

                var exitCode = process.ExitCode;
                result.Success = exitCode == 0;

                // Parse the output to extract details
                ParseVerifyReadmeOutput(output.ToString(), error.ToString(), result);

                if (!result.Success && !result.Details.Any())
                {
                    // If we failed but have no specific details, add a general error
                    result.Details.Add(new CICheckDetail
                    {
                        Severity = CICheckSeverity.Error,
                        Message = "README verification failed. Check logs for details.",
                        Rule = "general-failure"
                    });
                }

                result.Summary = result.Success 
                    ? $"README verification passed for {result.FilesChecked} files"
                    : $"README verification failed with {result.Details.Count} issues";

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing Verify-Readme script");
                result.Success = false;
                result.Summary = $"Script execution failed: {ex.Message}";
                result.Details.Add(new CICheckDetail
                {
                    Severity = CICheckSeverity.Critical,
                    Message = ex.Message,
                    Rule = "script-execution-error"
                });
                return result;
            }
        }

        private void ParseVerifyReadmeOutput(string output, string error, VerifyReadmeResult result)
        {
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var errorLines = error.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Combine output and error for parsing
            var allLines = lines.Concat(errorLines).ToArray();

            foreach (var line in allLines)
            {
                // Parse doc-warden output patterns
                if (TryParseDocWardenError(line, out var detail))
                {
                    result.Details.Add(detail);
                }
                else if (line.Contains("files scanned", StringComparison.OrdinalIgnoreCase))
                {
                    // Try to extract file count
                    var match = Regex.Match(line, @"(\d+)\s+files?\s+scanned", RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
                    {
                        result.FilesChecked = count;
                    }
                }
                else if (line.Contains("ERROR:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Details.Add(new CICheckDetail
                    {
                        Severity = CICheckSeverity.Error,
                        Message = line.Trim(),
                        Rule = "general-error"
                    });
                }
                else if (line.Contains("WARNING:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Details.Add(new CICheckDetail
                    {
                        Severity = CICheckSeverity.Warning,
                        Message = line.Trim(),
                        Rule = "general-warning"
                    });
                }
            }
        }

        private bool TryParseDocWardenError(string line, out CICheckDetail detail)
        {
            detail = new CICheckDetail();

            // Common doc-warden error patterns
            // Example: "README.md:15: Error: Missing required section"
            var errorPattern = @"^(.+?):(\d+):\s*(Error|Warning|Info):\s*(.+)$";
            var match = Regex.Match(line, errorPattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                detail.File = match.Groups[1].Value.Trim();
                if (int.TryParse(match.Groups[2].Value, out var lineNum))
                    detail.Line = lineNum;
                
                var severityStr = match.Groups[3].Value.ToLowerInvariant();
                detail.Severity = severityStr switch
                {
                    "error" => CICheckSeverity.Error,
                    "warning" => CICheckSeverity.Warning,
                    "info" => CICheckSeverity.Info,
                    _ => CICheckSeverity.Error
                };

                detail.Message = match.Groups[4].Value.Trim();
                detail.Rule = "doc-warden";
                return true;
            }

            return false;
        }

        private class VerifyReadmeResult
        {
            public bool Success { get; set; }
            public string Summary { get; set; } = string.Empty;
            public List<CICheckDetail> Details { get; set; } = new();
            public int FilesChecked { get; set; }
        }
    }
}
