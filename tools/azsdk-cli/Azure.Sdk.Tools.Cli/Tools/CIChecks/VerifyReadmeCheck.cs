// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Sdk.Tools.Cli.Models.Responses;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Azure.Sdk.Tools.Cli.Tools.CIChecks
{
    public class VerifyReadmeCheck : ICICheck
    {
        private readonly ILogger<VerifyReadmeCheck> logger;

        public string Name => "verify-readme";
        public string Description => "Verifies README.md files exist and contain required sections";

        public VerifyReadmeCheck(ILogger<VerifyReadmeCheck> logger)
        {
            this.logger = logger;
        }

        public async Task<CICheckResponse> RunCheckAsync(string rootPath, CancellationToken cancellationToken = default)
        {
            var response = new CICheckResponse
            {
                CheckName = Name,
                Status = CICheckStatus.Pass,
                Summary = "README verification completed"
            };

            var details = new List<CICheckDetail>();
            int filesChecked = 0;
            int issuesFound = 0;

            try
            {
                // Find all directories that should have README files
                var projectDirectories = await FindProjectDirectoriesAsync(rootPath, cancellationToken);
                
                foreach (var directory in projectDirectories)
                {
                    var readmeIssues = await CheckReadmeInDirectoryAsync(directory, cancellationToken);
                    details.AddRange(readmeIssues);
                    filesChecked++;
                    
                    if (readmeIssues.Any(d => d.Severity >= CICheckSeverity.Warning))
                    {
                        issuesFound++;
                    }
                }

                // Check root README
                var rootReadmeIssues = await CheckReadmeInDirectoryAsync(rootPath, cancellationToken);
                details.AddRange(rootReadmeIssues);
                filesChecked++;
                
                if (rootReadmeIssues.Any(d => d.Severity >= CICheckSeverity.Warning))
                {
                    issuesFound++;
                }

                // Determine overall status
                if (details.Any(d => d.Severity == CICheckSeverity.Critical || d.Severity == CICheckSeverity.Error))
                {
                    response.Status = CICheckStatus.Fail;
                }
                else if (details.Any(d => d.Severity == CICheckSeverity.Warning))
                {
                    response.Status = CICheckStatus.Warning;
                }

                response.Details = details;
                response.FilesChecked = filesChecked;
                response.IssuesFound = issuesFound;
                response.Summary = $"Checked {filesChecked} directories, found {issuesFound} issues";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running README verification check");
                response.Status = CICheckStatus.Error;
                response.Summary = $"Error during check: {ex.Message}";
                details.Add(new CICheckDetail
                {
                    File = rootPath,
                    Severity = CICheckSeverity.Error,
                    Message = $"Check execution failed: {ex.Message}",
                    Rule = "check-execution"
                });
                response.Details = details;
            }

            return response;
        }

        private async Task<List<string>> FindProjectDirectoriesAsync(string rootPath, CancellationToken cancellationToken)
        {
            var directories = new List<string>();
            
            // Look for common project indicators
            var projectIndicators = new[]
            {
                "*.csproj",
                "*.sln",
                "package.json",
                "pom.xml",
                "setup.py",
                "pyproject.toml",
                "Cargo.toml",
                "go.mod"
            };

            foreach (var pattern in projectIndicators)
            {
                var files = Directory.GetFiles(rootPath, pattern, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var dir = Path.GetDirectoryName(file);
                    if (dir != null && !directories.Contains(dir))
                    {
                        directories.Add(dir);
                    }
                }
            }

            // Also check common SDK directory patterns
            var commonSdkPaths = new[]
            {
                Path.Combine(rootPath, "sdk"),
                Path.Combine(rootPath, "src"),
                Path.Combine(rootPath, "packages"),
                Path.Combine(rootPath, "libs")
            };

            foreach (var path in commonSdkPaths)
            {
                if (Directory.Exists(path))
                {
                    var subdirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
                    directories.AddRange(subdirs);
                }
            }

            return directories.Distinct().ToList();
        }

        private async Task<List<CICheckDetail>> CheckReadmeInDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
        {
            var issues = new List<CICheckDetail>();
            var readmePath = Path.Combine(directoryPath, "README.md");

            // Check if README.md exists
            if (!File.Exists(readmePath))
            {
                issues.Add(new CICheckDetail
                {
                    File = directoryPath,
                    Severity = CICheckSeverity.Warning,
                    Message = "README.md file not found",
                    Rule = "readme-missing"
                });
                return issues;
            }

            try
            {
                var content = await File.ReadAllTextAsync(readmePath, cancellationToken);
                
                // Check for required sections
                var requiredSections = new Dictionary<string, string>
                {
                    { "# ", "Main heading" },
                    { "## Getting started", "Getting started section" },
                    { "## Key concepts", "Key concepts section" },
                    { "## Examples", "Examples section" },
                    { "## Troubleshooting", "Troubleshooting section" },
                    { "## Contributing", "Contributing section" }
                };

                foreach (var section in requiredSections)
                {
                    if (!content.Contains(section.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(new CICheckDetail
                        {
                            File = readmePath,
                            Severity = CICheckSeverity.Warning,
                            Message = $"Missing recommended section: {section.Value}",
                            Rule = "readme-missing-section"
                        });
                    }
                }

                // Check for basic content quality
                if (content.Length < 100)
                {
                    issues.Add(new CICheckDetail
                    {
                        File = readmePath,
                        Severity = CICheckSeverity.Warning,
                        Message = "README appears to be too short (less than 100 characters)",
                        Rule = "readme-too-short"
                    });
                }

                // Check for placeholder text
                var placeholderPatterns = new[]
                {
                    @"\[Your\s+\w+\]",
                    @"TODO:",
                    @"FIXME:",
                    @"\[Package Name\]",
                    @"\[Description\]"
                };

                foreach (var pattern in placeholderPatterns)
                {
                    var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        var lineNumber = GetLineNumber(content, match.Index);
                        issues.Add(new CICheckDetail
                        {
                            File = readmePath,
                            Line = lineNumber,
                            Severity = CICheckSeverity.Warning,
                            Message = $"Placeholder text found: {match.Value}",
                            Rule = "readme-placeholder-text"
                        });
                    }
                }

                // Check for broken links (basic check)
                var linkPattern = @"\[([^\]]+)\]\(([^)]+)\)";
                var linkMatches = Regex.Matches(content, linkPattern);
                foreach (Match match in linkMatches)
                {
                    var url = match.Groups[2].Value;
                    if (url.StartsWith("http") && url.Contains("example.com"))
                    {
                        var lineNumber = GetLineNumber(content, match.Index);
                        issues.Add(new CICheckDetail
                        {
                            File = readmePath,
                            Line = lineNumber,
                            Severity = CICheckSeverity.Warning,
                            Message = "Link points to example.com - likely a placeholder",
                            Rule = "readme-placeholder-link"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                issues.Add(new CICheckDetail
                {
                    File = readmePath,
                    Severity = CICheckSeverity.Error,
                    Message = $"Error reading README file: {ex.Message}",
                    Rule = "readme-read-error"
                });
            }

            return issues;
        }

        private static int GetLineNumber(string content, int index)
        {
            return content.Take(index).Count(c => c == '\n') + 1;
        }
    }
}
